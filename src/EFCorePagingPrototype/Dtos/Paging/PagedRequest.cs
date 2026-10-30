using EFCorePagingPrototype.Dtos.Enums;
using EFCorePagingPrototype.Dtos.Filtering;

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
    public List<PrimeTableFilterRequest> Filters { get; set; } = new();
}

public class PagedRequest2 {
    /// <summary>
    /// Number of records to skip (maps to PrimeNG lazy load 'first').
    /// </summary>
    public int Skip { get; set; } = 0;

    /// <summary>
    /// Requested record count per page (maps to PrimeNG lazy load 'rows').
    /// </summary>
    public virtual int PageSize { get; set; } = 20;

    /// <summary>
    /// Comma-separated string of properties to sort by.
    /// </summary>
    public virtual string SortBy { get; set; } = string.Empty;

    /// <summary>
    /// Sets the default sort direction.
    /// </summary>
    /// <remarks>Ascending or Descending</remarks>
    public virtual SortDirection SortDirection { get; set; } = SortDirection.Ascending;

    #region PrimeNG Global Filter Configuration

    /// <summary>
    /// Specifies how multiple global filters are combined.
    /// </summary>
    public FilterOperator GlobalFilterOperator { get; set; } = FilterOperator.All;

    /// <summary>
    /// Collection of filters to apply to the requested data.
    /// </summary>
    public List<PrimeTableFilterRequest> Filters { get; set; } = new();

    #endregion
}