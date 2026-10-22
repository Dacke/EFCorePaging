using EFCorePagingPrototype.Dtos.Enums;
using EFCorePagingPrototype.Helpers;
using System.Linq.Expressions;
using System.Reflection;

namespace EFCorePagingPrototype.Data.Extensions;

public static class QueryableKeysetExtensions {
    public static IQueryable<T> ApplyKeysetPaging<T>(this IQueryable<T> query, KeysetCursor? cursor, List<(string PropertyName, SortDirection Direction)> sortColumns, string primaryKeyName, NavigationDirection navDirection = NavigationDirection.Forward) {
        bool isBackward = navDirection == NavigationDirection.Backward;

        // Invert directions when navigating backward to seek top-bound items
        var effectiveSorts = isBackward
            ? sortColumns.Select(s => (s.PropertyName, Direction: InvertDirection(s.Direction))).ToList()
            : sortColumns.ToList();

        // 1. Build Dynamic Ordering
        query = ApplyOrdering(query, effectiveSorts, primaryKeyName);

        // 2. Build Dynamic Keyset Predicate if cursor is provided
        if (cursor != null && cursor.SortValues.Count > 0) {
            var predicate = BuildKeysetPredicate<T>(cursor, effectiveSorts, primaryKeyName);
            if (predicate != null)
                query = query.Where(predicate);
        }

        return query;
    }

    private static SortDirection InvertDirection(SortDirection dir) => dir == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending;

    private static IQueryable<T> ApplyOrdering<T>(
        IQueryable<T> query,
        List<(string PropertyName, SortDirection Direction)> sortColumns,
        string primaryKeyName) {
        var parameter = Expression.Parameter(typeof(T), "x");
        var isFirst = true;

        // Ensure tie-breaker PK is included if not specified
        var allSorts = sortColumns.ToList();
        if (allSorts.Any(s => s.PropertyName.Equals(primaryKeyName, StringComparison.OrdinalIgnoreCase)) == false) {
            var lastDirection = allSorts.LastOrDefault().Direction;
            allSorts.Add((primaryKeyName, lastDirection));
        }

        foreach (var (propName, direction) in allSorts) {
            var propInfo = typeof(T).GetProperty(propName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
                ?? throw new ArgumentException($"Property '{propName}' not found on type {typeof(T).Name}");

            var propertyAccess = Expression.Property(parameter, propInfo);
            var lambda = Expression.Lambda(propertyAccess, parameter);

            string methodName = isFirst
                ? (direction == SortDirection.Ascending ? "OrderBy" : "OrderByDescending")
                : (direction == SortDirection.Ascending ? "ThenBy" : "ThenByDescending");

            var resultExpression = Expression.Call(
                typeof(Queryable),
                methodName,
                [typeof(T), propInfo.PropertyType],
                query.Expression,
                Expression.Quote(lambda));

            query = query.Provider.CreateQuery<T>(resultExpression);
            isFirst = false;
        }

        return query;
    }

    private static Expression<Func<T, bool>>? BuildKeysetPredicate<T>(
        KeysetCursor cursor,
        List<(string PropertyName, SortDirection Direction)> sortColumns,
        string primaryKeyName) {
        var parameter = Expression.Parameter(typeof(T), "x");
        Expression? totalPredicate = null;

        var allSorts = sortColumns.ToList();
        if (allSorts.Any(s => s.PropertyName.Equals(primaryKeyName, StringComparison.OrdinalIgnoreCase)) == false) {
            var lastDir = allSorts.LastOrDefault().Direction;
            allSorts.Add((primaryKeyName, lastDir));
        }

        // Build OR branches for keyset evaluation
        for (int i = 0; i < allSorts.Count; i++) {
            Expression? branch = null;

            // 1. Equalities for preceding columns
            for (int j = 0; j < i; j++) {
                var (prevProp, _) = allSorts[j];
                var eqExpr = BuildComparison<T>(parameter, cursor, prevProp, ExpressionType.Equal);
                branch = branch == null ? eqExpr : Expression.AndAlso(branch, eqExpr);
            }

            // 2. Inequality for the target column
            var (targetProp, direction) = allSorts[i];
            var compType = direction == SortDirection.Ascending
                ? ExpressionType.GreaterThan
                : ExpressionType.LessThan;

            var compExpr = BuildComparison<T>(parameter, cursor, targetProp, compType);
            branch = branch == null ? compExpr : Expression.AndAlso(branch, compExpr);

            // Combine branches with OR
            totalPredicate = totalPredicate == null ? branch : Expression.OrElse(totalPredicate, branch);
        }

        if (totalPredicate == null)
            return null;

        return Expression.Lambda<Func<T, bool>>(totalPredicate, parameter);
    }

    private static Expression BuildComparison<T>(
        ParameterExpression parameter,
        KeysetCursor cursor,
        string propertyName,
        ExpressionType comparisonType) {
        var propInfo = typeof(T).GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
            ?? throw new ArgumentException($"Property '{propertyName}' on {typeof(T).Name} not found.");

        var left = Expression.Property(parameter, propInfo);

        string? stringVal = propertyName.Equals(cursor.PrimaryKeyName, StringComparison.OrdinalIgnoreCase)
            ? cursor.PrimaryKeyValue
            : cursor.SortValues.FirstOrDefault(kvp => kvp.Key.Equals(propertyName, StringComparison.OrdinalIgnoreCase)).Value;

        object? convertedVal = ConvertStringValue(stringVal, propInfo.PropertyType);
        var right = Expression.Constant(convertedVal, propInfo.PropertyType);

        var targetType = Nullable.GetUnderlyingType(propInfo.PropertyType) ?? propInfo.PropertyType;

        // 1. Equalities (works natively for all types)
        if (comparisonType == ExpressionType.Equal)
            return Expression.Equal(left, right);

        // 2. String handling: Use CompareTo so Expression Tree translates directly to SQL (LastName > @val)
        if (targetType == typeof(string)) {
            var compareToMethod = typeof(string).GetMethod(nameof(string.CompareTo), [typeof(string)])!;
            var compareCall = Expression.Call(left, compareToMethod, right);
            var zero = Expression.Constant(0);

            return comparisonType switch {
                ExpressionType.GreaterThan => Expression.GreaterThan(compareCall, zero),
                ExpressionType.GreaterThanOrEqual => Expression.GreaterThanOrEqual(compareCall, zero),
                ExpressionType.LessThan => Expression.LessThan(compareCall, zero),
                ExpressionType.LessThanOrEqual => Expression.LessThanOrEqual(compareCall, zero),
                _ => throw new NotSupportedException($"Comparison type {comparisonType} is not supported for string.")
            };
        }

        // 3. Guid handling
        if (targetType == typeof(Guid)) {
            var compareToMethod = typeof(Guid).GetMethod(nameof(Guid.CompareTo), [typeof(Guid)])!;
            var compareCall = Expression.Call(left, compareToMethod, right);
            var zero = Expression.Constant(0);

            return comparisonType switch {
                ExpressionType.GreaterThan => Expression.GreaterThan(compareCall, zero),
                ExpressionType.GreaterThanOrEqual => Expression.GreaterThanOrEqual(compareCall, zero),
                ExpressionType.LessThan => Expression.LessThan(compareCall, zero),
                ExpressionType.LessThanOrEqual => Expression.LessThanOrEqual(compareCall, zero),
                _ => throw new NotSupportedException($"Comparison type {comparisonType} is not supported for Guid.")
            };
        }

        // 4. Primitive types (int, long, DateTime, double, etc.)
        return Expression.MakeBinary(comparisonType, left, right);
    }

    private static object? ConvertStringValue(string? value, Type targetType) {
        if (value == null)
            return null;

        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlyingType == typeof(Guid))
            return Guid.Parse(value);

        if (underlyingType.IsEnum)
            return Enum.Parse(underlyingType, value);

        return Convert.ChangeType(value, underlyingType);
    }
}