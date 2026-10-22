using EFCorePagingPrototype.Dtos.Converter;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace EFCorePagingPrototype.Dtos.Enums;

[JsonConverter(typeof(EnumMemberJsonConverter<MatchMode>))]
public enum MatchMode {
    [EnumMember(Value = "startsWith")]
    StartsWith = 0,
    [EnumMember(Value = "contains")]
    Contains = 1,
    [EnumMember(Value = "notContains")]
    NotContains = 2,
    [EnumMember(Value = "endsWith")]
    EndsWith = 3,
    [EnumMember(Value = "equals")]
    Equals = 4,
    [EnumMember(Value = "notEquals")]
    NotEquals = 5,
    [EnumMember(Value = "gt")]
    GreaterThan = 6,
    [EnumMember(Value = "gte")]
    GreaterThanOrEqualTo = 7,
    [EnumMember(Value = "lt")]
    LessThan = 8,
    [EnumMember(Value = "lte")]
    LessThanOrEqualTo = 9
}