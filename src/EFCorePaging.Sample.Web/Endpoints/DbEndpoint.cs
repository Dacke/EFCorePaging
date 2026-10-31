using EFCorePaging.Sample.Web.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace EFCorePaging.Sample.Web.Endpoints;

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