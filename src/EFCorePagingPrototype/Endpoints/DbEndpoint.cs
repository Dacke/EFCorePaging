using EFCorePagingPrototype.Data;
using EFCorePagingPrototype.Data.Models;
using EFCorePagingPrototype.Dtos.Enums;
using EFCorePagingPrototype.Dtos.Paging;
using EFCorePagingPrototype.Helpers;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Net;
using System.Reflection;

namespace EFCorePagingPrototype.Endpoints;

public abstract class DbEndpointWithoutRequest<TResponse> : DbEndpoint<EmptyRequest, TResponse> {
    protected DbEndpointWithoutRequest(ITestDataContextFactory dbContextFactory) : base(dbContextFactory) { }
}

public abstract class DbEndpoint<TRequest> : DbEndpoint<TRequest, object?> where TRequest : notnull {
    protected DbEndpoint(ITestDataContextFactory dbContextFactory) : base(dbContextFactory) { }
}

public abstract partial class DbEndpoint<TRequest, TResponse> : Endpoint<TRequest, TResponse> where TRequest : notnull {
    protected readonly ITestDataContextFactory _dbContextFactory;

    public DbEndpoint(ITestDataContextFactory dbContextFactory) => _dbContextFactory = dbContextFactory;

    public override void Configure() {
        base.Configure();

        Description(builder => {
            builder.Accepts<TRequest>();
            if (typeof(TResponse) != typeof(void))
                builder.Produces<TResponse>();

            builder.ProducesProblemDetails((int)HttpStatusCode.BadRequest)
                   .ProducesProblemDetails((int)HttpStatusCode.Conflict)
                   .ProducesProblemDetails((int)HttpStatusCode.InternalServerError);
        });
    }

    protected async Task WithDbContextAsync(Func<ITestDataContext, Task> action, CancellationToken ct) {
        using (_dbContextFactory) {
            var context = _dbContextFactory.CreateDbContext();
            await action(context);
        }
    }

    protected async Task<TResult> WithDbContextAsync<TResult>(Func<ITestDataContext, Task<TResult>> action, CancellationToken ct) {
        using (_dbContextFactory) {
            var context = _dbContextFactory.CreateDbContext();
            return await action(context);
        }
    }

    protected async Task<bool> SaveDatabaseChangesAsync(ITestDataContext dbContext, CancellationToken cancellationToken, bool errorIfNoRowsAffected = true) {
        try {
            var rowsAffected = await dbContext.SaveChangesAsync(cancellationToken);
            if (errorIfNoRowsAffected == false)
                return true;

            if (rowsAffected == 0) {
                AddError("The database operation failed.  Please reload and try again.");
                await Send.ErrorsAsync((int)HttpStatusCode.InternalServerError, cancellationToken);
                return false;
            }
        }
        catch (DbUpdateConcurrencyException) {
            await SendConflictAsync(cancellationToken);
            return false;
        }
        catch (Exception ex) {
            AddError(ex.Message);
            await Send.ErrorsAsync((int)HttpStatusCode.InternalServerError, cancellationToken);
            return false;
        }

        return true;
    }

    protected async Task SendConflictAsync(CancellationToken ct) => await Send.StatusCodeAsync((int)HttpStatusCode.Conflict, ct);

    protected async Task SendBadRequestAsync(string message, CancellationToken ct) {
        AddError(message);
        await Send.ErrorsAsync((int)HttpStatusCode.BadRequest, ct);
    }
}

public abstract class ExtendedDbEndpoint<TRequest> : DbEndpoint<TRequest> where TRequest : notnull {
    protected ExtendedDbEndpoint(ITestDataContextFactory dbContextFactory) : base(dbContextFactory) { }

    public override async Task HandleAsync(TRequest req, CancellationToken ct) {
        using (_dbContextFactory) {
            var db = _dbContextFactory.CreateDbContext();
            await HandleWithDbAsync(db, req, ct);
        }
    }

    protected abstract Task HandleWithDbAsync(ITestDataContext db, TRequest req, CancellationToken ct);
}

public abstract partial class ExtendedDbEndpoint<TRequest, TResponse> : DbEndpoint<TRequest, TResponse> where TRequest : notnull {
    protected ExtendedDbEndpoint(ITestDataContextFactory dbContextFactory) : base(dbContextFactory) { }

    public override async Task HandleAsync(TRequest req, CancellationToken ct) {
        try {
            using (_dbContextFactory) {
                var db = _dbContextFactory.CreateDbContext();
                await HandleWithDbAsync(db, req, ct);
            }
        }
        catch (Exception ex) {
            AddError(ex.Message);
            await Send.ErrorsAsync((int)HttpStatusCode.InternalServerError, ct);
        }
    }

    protected abstract Task HandleWithDbAsync(ITestDataContext db, TRequest req, CancellationToken ct);
}

public abstract class PagedDbEndpoint<TRequest, TQueryResult, TResponse> : DbEndpoint<TRequest, TResponse>
    where TRequest : PagedRequest
    where TQueryResult : class
    where TResponse : PagedResponse<TQueryResult>, new() {

    private static readonly ConcurrentDictionary<(Type Type, string PropertyName), PropertyInfo?> PropertyCache = new();

    protected abstract string DefaultSortColumn { get; }
    protected abstract string EntityKeyColumn { get; }
    protected virtual string DtoKeyColumn => EntityKeyColumn;

    protected PagedDbEndpoint(ITestDataContextFactory dbContextFactory) : base(dbContextFactory) { }

    public override async Task HandleAsync(TRequest req, CancellationToken ct) {
        try {
            using var db = _dbContextFactory.CreateDbContext();

            ArgumentException.ThrowIfNullOrWhiteSpace(DefaultSortColumn);
            ArgumentException.ThrowIfNullOrWhiteSpace(EntityKeyColumn);

            int pageSize = req.PageSize is < 1 or > 100 ? 20 : req.PageSize;
            var cursor = CursorHelper.DecodeCursor<KeysetCursor>(req.Cursor);
            var sortColumns = SortParser.ParseSortColumns(req.SortBy, req.SortDirection, defaultColumn: DefaultSortColumn);

            var pagedContext = new PagingContext(pageSize, cursor, sortColumns, EntityKeyColumn, req.NavigationDirection);
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

        var propInfo = GetCachedProperty(type, DtoKeyColumn)
                    ?? GetCachedProperty(type, "Id")
                    ?? throw new InvalidOperationException($"Property '{DtoKeyColumn}' or 'Id' not found on type {type.Name}.");

        return propInfo.GetValue(item) ?? throw new InvalidOperationException($"Primary key value for property '{propInfo.Name}' on {type.Name} cannot be null.");
    }

    private string? BuildCursorForBoundary(bool canNavigate, List<(string PropertyName, SortDirection Direction)> sortColumns, IList<TQueryResult> items, bool isLastItem) {

        if (canNavigate == false || items.Count <= 0)
            return null;

        var targetItem = isLastItem ? items.Last() : items.First();
        var itemType = typeof(TQueryResult);

        var sortValues = sortColumns.Select(s => {
            var dtoPropName = s.PropertyName.Equals(EntityKeyColumn, StringComparison.OrdinalIgnoreCase)
                ? DtoKeyColumn
                : s.PropertyName;

            var propInfo = GetCachedProperty(itemType, dtoPropName);
            var propVal = propInfo?.GetValue(targetItem);

            return (s.PropertyName, propVal);
        });

        object pkValue = GetPrimaryKeyValue(targetItem);
        var newCursor = new KeysetCursor(EntityKeyColumn, pkValue, sortValues);

        return CursorHelper.EncodeCursor(newCursor);
    }

    private static PropertyInfo? GetCachedProperty(Type type, string propertyName) {
        return PropertyCache.GetOrAdd((type, propertyName), key =>
            key.Type.GetProperty(key.PropertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance));
    }
}

public record PagingContext(
    int PageSize,
    KeysetCursor? Cursor,
    List<(string PropertyName, SortDirection Direction)> SortColumns,
    string PrimaryKeyColumn,
    NavigationDirection NavigationDirection = NavigationDirection.Forward);