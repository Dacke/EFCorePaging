using EFCorePaging.Offset.Enums.Converters;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace EFCorePaging.Offset.Enums;

[JsonConverter(typeof(EnumMemberJsonConverter<SortDirection>))]
public enum FilterOperator {
    [EnumMember(Value = "all, and")]
    All = 0,
    [EnumMember(Value = "any, or")]
    Any = 1
}