using EFCorePagingPrototype.Dtos.Enums;
using EFCorePagingPrototype.Dtos.Filtering;
using System.Linq.Expressions;
using System.Reflection;

namespace EFCorePagingPrototype.Data.Extensions;

public static class QueryableFilterExtensions {
    private static readonly MethodInfo StartsWithMethod = typeof(string).GetMethod(nameof(string.StartsWith), new[] { typeof(string) })!;
    private static readonly MethodInfo EndsWithMethod = typeof(string).GetMethod(nameof(string.EndsWith), new[] { typeof(string) })!;
    private static readonly MethodInfo ContainsMethod = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;

    public static IQueryable<T> ApplyPrimeFilters<T>(this IQueryable<T> query, List<PrimeTableFilterRequest>? filters, FilterOperator globalOperator = FilterOperator.All) {
        if (filters == null || filters.Count == 0)
            return query;

        var parameter = Expression.Parameter(typeof(T), "x");
        Expression? rootPredicate = null;

        foreach (var fieldFilter in filters) {
            if (fieldFilter.Constraints == null || fieldFilter.Constraints.Count == 0)
                continue;

            Expression? fieldPredicate = null;
            bool isFieldAnd = fieldFilter.Operator == FilterOperator.All;

            // Build rules for a single column/field
            foreach (var rule in fieldFilter.Constraints) {
                if (string.IsNullOrWhiteSpace(rule.PropertyName) || string.IsNullOrWhiteSpace(rule.Value))
                    continue;

                var ruleExpr = BuildRuleExpression<T>(parameter, rule);
                if (ruleExpr == null)
                    continue;

                if (fieldPredicate == null) {
                    fieldPredicate = ruleExpr;
                }
                else {
                    fieldPredicate = isFieldAnd
                        ? Expression.AndAlso(fieldPredicate, ruleExpr)
                        : Expression.OrElse(fieldPredicate, ruleExpr);
                }
            }

            if (fieldPredicate == null)
                continue;

            // Combine across different columns
            bool isGlobalAnd = globalOperator == FilterOperator.All;
            if (rootPredicate == null) {
                rootPredicate = fieldPredicate;
            }
            else {
                rootPredicate = isGlobalAnd
                    ? Expression.AndAlso(rootPredicate, fieldPredicate)
                    : Expression.OrElse(rootPredicate, fieldPredicate);
            }
        }

        if (rootPredicate == null)
            return query;

        var lambda = Expression.Lambda<Func<T, bool>>(rootPredicate, parameter);
        return query.Where(lambda);
    }

    private static Expression? BuildRuleExpression<T>(ParameterExpression parameter, ColumnFilterMetaData rule) {
        var propInfo = typeof(T).GetProperty(rule.PropertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        if (propInfo == null)
            return null;

        var propertyAccess = Expression.Property(parameter, propInfo);
        var targetType = Nullable.GetUnderlyingType(propInfo.PropertyType) ?? propInfo.PropertyType;

        object? convertedVal;
        try {
            if (targetType == typeof(Guid))
                convertedVal = Guid.Parse(rule.Value);
            else
                convertedVal = Convert.ChangeType(rule.Value, targetType);
        }
        catch {
            return null; // Ignore invalid format conversions
        }

        var constant = Expression.Constant(convertedVal, propInfo.PropertyType);

        // String specific operations
        if (targetType == typeof(string)) {
            return rule.MatchMode switch {
                MatchMode.StartsWith => Expression.Call(propertyAccess, StartsWithMethod, constant),
                MatchMode.EndsWith => Expression.Call(propertyAccess, EndsWithMethod, constant),
                MatchMode.Contains => Expression.Call(propertyAccess, ContainsMethod, constant),
                MatchMode.NotContains => Expression.Not(Expression.Call(propertyAccess, ContainsMethod, constant)),
                MatchMode.Equals => Expression.Equal(propertyAccess, constant),
                MatchMode.NotEquals => Expression.NotEqual(propertyAccess, constant),
                _ => Expression.Equal(propertyAccess, constant)
            };
        }

        // Numeric / Date / Boolean comparison operations
        return rule.MatchMode switch {
            MatchMode.Equals => Expression.Equal(propertyAccess, constant),
            MatchMode.NotEquals => Expression.NotEqual(propertyAccess, constant),
            MatchMode.GreaterThan => Expression.GreaterThan(propertyAccess, constant),
            MatchMode.GreaterThanOrEqualTo => Expression.GreaterThanOrEqual(propertyAccess, constant),
            MatchMode.LessThan => Expression.LessThan(propertyAccess, constant),
            MatchMode.LessThanOrEqualTo => Expression.LessThanOrEqual(propertyAccess, constant),
            _ => Expression.Equal(propertyAccess, constant)
        };
    }
}