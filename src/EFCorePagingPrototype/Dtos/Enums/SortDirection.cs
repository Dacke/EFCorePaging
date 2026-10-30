using EFCorePagingPrototype.Dtos.Converter;
using System.Text.Json.Serialization;

namespace EFCorePagingPrototype.Dtos.Enums;

[JsonConverter(typeof(EnumMemberJsonConverter<SortDirection>))]
public enum SortDirection {
    Ascending,
    Descending
}