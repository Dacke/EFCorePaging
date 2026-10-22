using EFCorePagingPrototype.Data;
using EFCorePagingPrototype.Data.Extensions;
using EFCorePagingPrototype.Data.Models;
using EFCorePagingPrototype.Dtos.Paging;
using EFCorePagingPrototype.Endpoints;
using EFCorePagingPrototype.Helpers;
using Mapster;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

public class PeopleSearchEndpoint : ExtendedDbEndpoint<PeopleRequest, PeopleResponse> {
    public PeopleSearchEndpoint(ITestDataContextFactory dbContextFactory) : base(dbContextFactory) { }

    public override void Configure() => Post("people/search");

    protected override async Task HandleWithDbAsync(ITestDataContext db, PeopleRequest req, CancellationToken ct) {
        int pageSize = req.PageSize is < 1 or > 100 ? 20 : req.PageSize;
        var cursor = CursorHelper.DecodeCursor<KeysetCursor>(req.Cursor);
        var sortColumns = SortParser.ParseSortColumns(req.SortBy, req.SortDirection, defaultColumn: nameof(Person.PersonId));

        const string primaryKeyName = nameof(Person.PersonId);

        var personDtos = await db.People
                                 .AsNoTracking()
                                 .ApplyPrimeFilters(req.Filters, req.GlobalFilterOperator)
                                 .ApplyKeysetPaging(cursor, sortColumns, primaryKeyName)
                                 .Take(pageSize + 1)
                                 .ProjectToType<PersonDto>()
                                 .ToListAsync(ct);

        bool hasMore = personDtos.Count > pageSize;
        if (hasMore)
            personDtos.RemoveAt(personDtos.Count - 1);
        
        string? nextCursor = null;
        if (hasMore && personDtos.Count > 0) {
            var lastDto = personDtos.Last();
            var sortValues = sortColumns.Select(s => {
                var dtoPropName = s.PropertyName.Equals(nameof(Person.PersonId), StringComparison.OrdinalIgnoreCase)
                    ? nameof(PersonDto.Id)
                    : s.PropertyName;

                var propVal = typeof(PersonDto)
                    .GetProperty(dtoPropName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
                    ?.GetValue(lastDto);

                return (s.PropertyName, propVal);
            });

            var newCursor = new KeysetCursor(primaryKeyName, lastDto.Id, sortValues);
            nextCursor = CursorHelper.EncodeCursor(newCursor);
        }

        var response = new PeopleResponse(personDtos, nextCursor, null, hasMore, false);
        await Send.OkAsync(response, ct);
    }
}

public class PeopleSearchRequest : PagedRequest {
    public override string SortBy { get; set; } = $"{nameof(PersonDto.LastName)}, {nameof(PersonDto.FirstName)}";
}