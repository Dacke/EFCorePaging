using EFCorePagingPrototype.Data;
using EFCorePagingPrototype.Data.Extensions;
using EFCorePagingPrototype.Data.Models;
using EFCorePagingPrototype.Dtos.Paging;
using EFCorePagingPrototype.Endpoints;
using Mapster;
using Microsoft.EntityFrameworkCore;

public class PeopleSearchEndpoint : ExtendedDbEndpoint<PeopleSearchRequest, PagedResponse2<PersonDto>> {
    public PeopleSearchEndpoint(ITestDataContextFactory dbContextFactory) : base(dbContextFactory) { }
        
    public override void Configure() => Post("people/search");

    protected override async Task HandleWithDbAsync(ITestDataContext db, PeopleSearchRequest req, CancellationToken ct) {
        var baseQuery = db.People
                          .AsNoTracking()
                          .ApplyPrimeFilters(req.Filters, req.GlobalFilterOperator);

        // 1. Get total count for PrimeNG pag-inator UI
        var totalCount = await baseQuery.CountAsync(ct);

        // 2. Define DTO -> Entity property mapping (e.g. DTO "Id" maps to Entity "PersonId")
        var propertyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { nameof(PersonDto.Id), nameof(Person.PersonId) }
        };

        // 3. Apply sorting and offset paging
        var personDtos = await baseQuery
                         .ApplyOffsetSorting(req.SortBy, req.SortDirection, propertyMap)
                         .Skip(req.Skip)
                         .Take(req.PageSize)
                         .ProjectToType<PersonDto>()
                         .ToListAsync(ct);
        
        var response = new PagedResponse2<PersonDto>(personDtos, totalCount);

        await Send.OkAsync(response, ct);
    }

    /*
    protected override async Task<IList<PersonDto>> HandlePagedWithDbAsync(ITestDataContext db, PeopleRequest req, PagingContext pagingContext, CancellationToken ct) {
        //  TODO: Figure out a way to determine if there is a DTO to Entity name map.
        var personDtos = await db.People
                                 .AsNoTracking()
                                 .ApplyPrimeFilters(req.Filters, req.GlobalFilterOperator)
                                 .ApplyKeysetPaging(pagingContext.Cursor, pagingContext.SortColumns, pagingContext.PrimaryKeyColumn, pagingContext.NavigationDirection)
                                 .Take(pagingContext.PageSize + 1)
                                 .ProjectToType<PersonDto>()
                                 .ToListAsync(ct);

        return personDtos;
    }
    */


}

public class PeopleSearchRequest : PagedRequest2 {
    public override string SortBy { get; set; } = $"{nameof(PersonDto.LastName)}, {nameof(PersonDto.FirstName)}";
}