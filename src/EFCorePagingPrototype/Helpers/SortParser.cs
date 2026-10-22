using EFCorePagingPrototype.Dtos.Enums;

namespace EFCorePagingPrototype.Helpers;

public static class SortParser {
    public static List<(string PropertyName, SortDirection Direction)> ParseSortColumns(
        string? sortBy,
        SortDirection defaultDirection = SortDirection.Ascending,
        string defaultColumn = "LastName") {
        var result = new List<(string PropertyName, SortDirection Direction)>();

        if (string.IsNullOrWhiteSpace(sortBy)) {
            result.Add((defaultColumn, defaultDirection));
            return result;
        }

        // Split by comma for multi-column sort e.g., "LastName ASC, FirstName DESC"
        var segments = sortBy.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var segment in segments) {
            var parts = segment.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            string propName = parts[0];

            SortDirection direction = defaultDirection;

            // Check if direction is explicitly defined in the segment
            if (parts.Length > 1) {
                if (parts[1].Equals("DESC", StringComparison.OrdinalIgnoreCase)) {
                    direction = SortDirection.Descending;
                }
                else if (parts[1].Equals("ASC", StringComparison.OrdinalIgnoreCase)) {
                    direction = SortDirection.Ascending;
                }
            }

            result.Add((propName, direction));
        }

        return result;
    }
}