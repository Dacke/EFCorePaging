using EFCorePaging.Offset.Enums;
using EFCorePaging.Offset.Filtering;
using EFCorePagingPrototype.Dtos.Enums;

namespace EFCorePagingPrototype.Dtos.Paging;

public class PagedRequest {
    /// <summary>
    /// Opaque Base64 string containing encoded KeysetCursor JSON metadata.
    /// Null for the initial page request.
    /// </summary>
    public string? Cursor { get; set; }

    public NavigationDirection NavigationDirection { get; set; } = NavigationDirection.Forward;

    //  TODO: Skip Property (gimmie)?

    /// <summary>
    /// Requested record count per segment (defaults to 20).
    /// </summary>
    public virtual int PageSize { get; set; } = 20;         //  RecordSize (this many)?

    /// <summary>
    /// Name of the main entity property to sort by (e.g., "LastName", "Age", "State").
    /// </summary>
    public virtual string SortBy { get; set; } = string.Empty;

    /// <summary>
    /// Ascending or Descending sort direction.
    /// </summary>
    public virtual SortDirection SortDirection { get; set; } = SortDirection.Ascending; //  TODO: No longer needed

    // PrimeNG Global Filter Configuration
    // "all"; // "all" (AND) | "any" (OR)    
    public FilterOperator GlobalFilterOperator { get; set; } = FilterOperator.All;
    public List<FilterRequest> Filters { get; set; } = new();
}