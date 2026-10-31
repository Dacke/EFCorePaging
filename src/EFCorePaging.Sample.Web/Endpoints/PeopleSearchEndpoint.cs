using EFCorePaging.Common.Data;
using EFCorePaging.Offset;
using EFCorePaging.Offset.Data;
using EFCorePaging.Sample.Web.Data;
using EFCorePaging.Sample.Web.Data.Models;
using EFCorePaging.Sample.Web.Dtos;
using EFCorePaging.Sample.Web.Endpoints;
using Mapster;
using Microsoft.EntityFrameworkCore;

public class PeopleSearchEndpoint : ExtendedDbEndpoint<PeopleSearchRequest, PagedResponse<PersonDto>> {
    public PeopleSearchEndpoint(ITestDataContextFactory dbContextFactory) : base(dbContextFactory) { }
        
    public override void Configure() => Post("people/search");

    protected override async Task HandleWithDbAsync(ITestDataContext db, PeopleSearchRequest req, CancellationToken ct) {
        var baseQuery = db.People
                          .AsNoTracking()
                          .ApplyFilters(req.Filters, req.GlobalFilterOperator);
                
        var totalCount = await baseQuery.CountAsync(ct);

        // Define DTO -> Entity property mapping (e.g. DTO "Id" maps to Entity "PersonId")
        var propertyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { nameof(PersonDto.Id), nameof(Person.PersonId) }
        };

        // Apply sorting and offset paging
        var personDtos = await baseQuery
                         .ApplySorting(req.SortBy, req.DefaultSortDirection, propertyMap)
                         .Skip(req.Skip)
                         .Take(req.PageSize)
                         .ProjectToType<PersonDto>()
                         .ToListAsync(ct);
        
        var response = new PagedResponse<PersonDto>(personDtos, totalCount);

        await Send.OkAsync(response, ct);
    }
}

public class PeopleSearchRequest : PagedRequest {
    public override string SortBy { get; set; } = $"{nameof(PersonDto.LastName)}, {nameof(PersonDto.FirstName)}";
}