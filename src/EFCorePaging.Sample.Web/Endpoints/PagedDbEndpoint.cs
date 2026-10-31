using EFCorePaging.Common.Enums;
using EFCorePaging.Common.Sorting;
using EFCorePaging.Cursor;
using EFCorePaging.Cursor.Enums;
using EFCorePaging.Cursor.Helpers;
using EFCorePaging.Sample.Web.Data;
using System.Collections.Concurrent;
using System.Net;
using System.Reflection;

namespace EFCorePaging.Sample.Web.Endpoints;

public abstract class PagedDbEndpoint<TRequest, TQueryResult, TResponse> : DbEndpoint<TRequest, TResponse>
    where TRequest : PagedRequest
    where TQueryResult : class
    where TResponse : PagedResponse<TQueryResult>, new() {

    private static readonly ConcurrentDictionary<(Type Type, string PropertyName), PropertyInfo?> PropertyCache = new();

    protected abstract PagingMetadata PagingMetadata { get; }

    protected PagedDbEndpoint(ITestDataContextFactory dbContextFactory) : base(dbContextFactory) { }

    public override async Task HandleAsync(TRequest req, CancellationToken ct) {
        try {
            using var db = _dbContextFactory.CreateDbContext();

            ArgumentException.ThrowIfNullOrWhiteSpace(PagingMetadata.DefaultSortColumn);
            ArgumentException.ThrowIfNullOrWhiteSpace(PagingMetadata.EntityKeyColumn);

            int pageSize = req.PageSize is < 1 or > 100 ? 20 : req.PageSize;
            var cursor = CursorHelper.DecodeCursor<KeysetCursor>(req.Cursor);
            var sortColumns = SortParser.ParseSortColumns(req.SortBy, req.DefaultSortDirection, defaultColumn: PagingMetadata.DefaultSortColumn);

            var pagedContext = new PagingContext(pageSize, cursor, sortColumns, PagingMetadata.EntityKeyColumn, req.NavigationDirection);
            var results = await HandlePagedWithDbAsync(db, req, pagedContext, ct);

            bool isBackward = req.NavigationDirection == NavigationDirection.Backward;
            bool hasBoundaryMatch = results.Count > pageSize;

            if (hasBoundaryMatch)
                results.RemoveAt(results.Count - 1);

            if (isBackward)
                results = results.Reverse().ToList();

            bool hasMore = isBackward ? (cursor != null) : hasBoundaryMatch;
            bool hasPrevious = isBackward ? hasBoundaryMatch : (cursor != null);

            string? nextCursor = BuildCursorForBoundary(hasMore, sortColumns, results, isLastItem: true);
            string? previousCursor = BuildCursorForBoundary(hasPrevious, sortColumns, results, isLastItem: false);

            var response = new TResponse {
                Values = results.ToArray(),
                NextCursor = nextCursor,
                PreviousCursor = previousCursor,
                HasMore = hasMore,
                HasPrevious = hasPrevious
            };

            await SendResponseAsync(response, ct);
        }
        catch (Exception ex) {
            AddError(ex.Message);
            await Send.ErrorsAsync((int)HttpStatusCode.InternalServerError, ct);
        }
    }

    protected virtual Task SendResponseAsync(TResponse response, CancellationToken ct) => Send.OkAsync(response, ct);

    protected abstract Task<IList<TQueryResult>> HandlePagedWithDbAsync(ITestDataContext db, TRequest req, PagingContext pagingContext, CancellationToken ct);

    protected virtual object GetPrimaryKeyValue(TQueryResult item) {
        var type = typeof(TQueryResult);

        var propInfo = GetCachedProperty(type, PagingMetadata.DtoKeyColumn)
                    ?? GetCachedProperty(type, "Id")
                    ?? throw new InvalidOperationException($"Property '{PagingMetadata.DtoKeyColumn}' or 'Id' not found on type {type.Name}.");

        return propInfo.GetValue(item) ?? throw new InvalidOperationException($"Primary key value for property '{propInfo.Name}' on {type.Name} cannot be null.");
    }

    private string? BuildCursorForBoundary(bool canNavigate, List<(string PropertyName, SortDirection Direction)> sortColumns, IList<TQueryResult> items, bool isLastItem) {

        if (canNavigate == false || items.Count <= 0)
            return null;

        var targetItem = isLastItem ? items.Last() : items.First();
        var itemType = typeof(TQueryResult);

        var sortValues = sortColumns.Select(s => {
            var dtoPropName = s.PropertyName.Equals(PagingMetadata.EntityKeyColumn, StringComparison.OrdinalIgnoreCase)
                ? PagingMetadata.DtoKeyColumn
                : s.PropertyName;

            var propInfo = GetCachedProperty(itemType, dtoPropName);
            var propVal = propInfo?.GetValue(targetItem);

            return (s.PropertyName, propVal);
        });

        object pkValue = GetPrimaryKeyValue(targetItem);
        var newCursor = new KeysetCursor(PagingMetadata.EntityKeyColumn, pkValue, sortValues);

        return CursorHelper.EncodeCursor(newCursor);
    }

    private static PropertyInfo? GetCachedProperty(Type type, string propertyName) {
        return PropertyCache.GetOrAdd((type, propertyName), key =>
            key.Type.GetProperty(key.PropertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance));
    }
}

public record PagingMetadata(
    string DefaultSortColumn,
    string EntityKeyColumn,
    string DtoKeyColumn);

public record PagingContext(
    int PageSize,
    KeysetCursor? Cursor,
    List<(string PropertyName, SortDirection Direction)> SortColumns,
    string PrimaryKeyColumn,
    NavigationDirection NavigationDirection = NavigationDirection.Forward);