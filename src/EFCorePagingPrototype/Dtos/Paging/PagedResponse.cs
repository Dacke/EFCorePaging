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

public class PagedResponse2<T> {
    /// <summary>
    /// Total number of records matching the filter criteria. Required for PrimeNG paginator.
    /// </summary>
    public int TotalCount { get; set; }

    public T[] Values { get; set; } = Array.Empty<T>();

    public PagedResponse2() { }

    public PagedResponse2(IEnumerable<T> values, int totalCount) {
        Values = values.ToArray();
        TotalCount = totalCount;
    }
}