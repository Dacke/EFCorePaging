namespace EFCorePagingPrototype.Dtos.Paging;

public class PagedResponse<T> {
    //  TODO: Get a total count of the values that match the filter.
    public T[] Values { get; set; } = Array.Empty<T>();

    //  TODO: Possibly going away
    public string? NextCursor { get; set; }
    public string? PreviousCursor { get; set; }
    public bool HasMore { get; set; }
    public bool HasPrevious { get; set; }

    public PagedResponse() { }

    public PagedResponse(IEnumerable<T> values, string? nextCursor = null, string? previousCursor = null, bool hasMore = false, bool hasPrevious = false) {
        Values = values.ToArray();

        HasMore = hasMore;
        NextCursor = nextCursor;

        HasPrevious = hasPrevious;
        PreviousCursor = previousCursor;
    }
}