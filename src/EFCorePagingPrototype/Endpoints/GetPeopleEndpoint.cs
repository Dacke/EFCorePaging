using EFCorePagingPrototype.Data;
using EFCorePagingPrototype.Data.Extensions;
using EFCorePagingPrototype.Data.Models;
using EFCorePagingPrototype.Dtos.Paging;
using FastEndpoints;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace EFCorePagingPrototype.Endpoints;

public class GetPeopleEndpoint : PagedDbEndpoint<PeopleRequest, PersonDto, PeopleResponse> {
    public GetPeopleEndpoint(ITestDataContextFactory dbContextFactory) : base(dbContextFactory) { }

    protected override string DefaultSortColumn => nameof(Person.PersonId);
    protected override string EntityKeyColumn => nameof(Person.PersonId);
    protected override string DtoKeyColumn => nameof(PersonDto.Id);

    public override void Configure() => Get("people");

    protected override async Task<IList<PersonDto>> HandlePagedWithDbAsync(ITestDataContext db, PeopleRequest req, PagingContext pagingContext, CancellationToken ct) {
        var personDtos = await db.People
            .AsNoTracking()
            .ApplyKeysetPaging(pagingContext.Cursor, pagingContext.SortColumns, pagingContext.PrimaryKeyColumn, pagingContext.NavigationDirection)
            .Take(pagingContext.PageSize + 1)
            .ProjectToType<PersonDto>()
            .ToListAsync(ct);

        return personDtos;
    }
}

public class PeopleRequest : PagedRequest {
    [QueryParam]
    public override string SortBy { get; set; } = nameof(PersonDto.LastName);
}


public class PeopleResponse : PagedResponse<PersonDto> {
    public PeopleResponse() { }
    public PeopleResponse(IEnumerable<PersonDto> values, string? nextCursor = null, string? previousCursor = null, bool hasMore = false, bool hasPrevious = false) 
        : base(values, nextCursor, previousCursor, hasMore, hasPrevious) { }
}
