using System.Text.Json.Serialization;

namespace EFCorePagingPrototype.Dtos.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NavigationDirection {
    Forward = 0, 
    Backward = 1
}
