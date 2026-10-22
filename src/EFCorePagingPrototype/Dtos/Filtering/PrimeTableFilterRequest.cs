using EFCorePagingPrototype.Dtos.Enums;

namespace EFCorePagingPrototype.Dtos.Filtering;

public class PrimeTableFilterRequest {
    /// <summary>
    /// Global or Column-level match operator ("all" = AND, "any" = OR).
    /// </summary>
    public FilterOperator Operator { get; set; } = FilterOperator.All;

    /// <summary>
    /// Rules per field name (e.g., "country", "name", "age").
    /// </summary>
    public List<ColumnFilterMetaData> Constraints { get; set; } = new();
}