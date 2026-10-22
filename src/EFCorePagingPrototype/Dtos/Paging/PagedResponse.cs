namespace EFCorePagingPrototype.Dtos.Paging;

public class PagedResponse<T> {
    public T[] Values { get; set; } = Array.Empty<T>();
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