using System.Text.Json.Serialization;

namespace EFCorePagingPrototype.Dtos.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FilterOperator {
    All = 0, // AND
    Any = 1  // OR
}