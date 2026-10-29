using EFCorePagingPrototype.Data;
using EFCorePagingPrototype.Data.Extensions;
using EFCorePagingPrototype.Data.Models;
using EFCorePagingPrototype.Dtos.Paging;
using EFCorePagingPrototype.Endpoints;
using Mapster;
using Microsoft.EntityFrameworkCore;

public class PeopleSearchEndpoint : PagedDbEndpoint<PeopleRequest, PersonDto, PeopleResponse> {
    public PeopleSearchEndpoint(ITestDataContextFactory dbContextFactory) : base(dbContextFactory) { }

    protected override PagingMetadata PagingMetadata => new(nameof(Person.PersonId),
                                                        nameof(Person.PersonId),
                                                        nameof(PersonDto.Id));

    public override void Configure() => Post("people/search");

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
}

public class PeopleSearchRequest : PagedRequest {
    public override string SortBy { get; set; } = $"{nameof(PersonDto.LastName)}, {nameof(PersonDto.FirstName)}";
}