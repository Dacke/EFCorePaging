using EFCorePaging.Sample.Web.Data;
using FastEndpoints;
using FluentValidation.Results;

namespace EFCorePaging.Sample.Web.Endpoints;

public class ExploreProblemDetailsEndpoint : ExtendedDbEndpoint<EmptyRequest> {
    public ExploreProblemDetailsEndpoint(ITestDataContextFactory dbContextFactory) : base(dbContextFactory) { }

    public override void Configure() => Get("problem/details");

    protected override async Task HandleWithDbAsync(ITestDataContext db, EmptyRequest req, CancellationToken ct) {

        ValidationFailures.AddRange([
            new ValidationFailure { 
                AttemptedValue = 42,
                ErrorCode = "ERROR_42",
                ErrorMessage = "Sample Error 42 Message",
                PropertyName = "Foo",
                Severity = FluentValidation.Severity.Error,
                CustomState = new { ErrCode = 42, Msg = "Fooled Ya", ThrownAt = DateTime.UtcNow },
            },
            new ValidationFailure {
                AttemptedValue = 42,
                ErrorCode = "ERROR_42",
                ErrorMessage = "Slightly Different Error 42 Message",
                PropertyName = "Foo",
                Severity = FluentValidation.Severity.Error,
                CustomState = "Sample String as Custom State",
            },
            new ValidationFailure {
                AttemptedValue = 42,
                ErrorCode = "ERROR_52",
                ErrorMessage = "Error 52 Message",
                PropertyName = "Bar",
                Severity = FluentValidation.Severity.Warning,
            }
        ]);
        ThrowIfAnyErrors();

        await Send.OkAsync(ct);
    }
}
