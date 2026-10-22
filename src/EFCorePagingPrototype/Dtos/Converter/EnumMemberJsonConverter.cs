using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EFCorePagingPrototype.Dtos.Converter;

/// <summary>
/// Reusable JsonConverter that maps enum values using [EnumMember(Value = "...")] attributes.
/// </summary>
public class EnumMemberJsonConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum {
    private static readonly Dictionary<string, TEnum> ToEnum = new(StringComparer.OrdinalIgnoreCase);
#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
    private static readonly Dictionary<TEnum, string> ToString = new();
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword

    static EnumMemberJsonConverter() {
        var type = typeof(TEnum);
        foreach (var value in Enum.GetValues<TEnum>()) {
            var memInfo = type.GetMember(value.ToString());
            var attr = memInfo[0].GetCustomAttribute<EnumMemberAttribute>();
            var name = attr?.Value ?? value.ToString();

            ToEnum[name] = value;
            ToString[value] = name;
        }
    }

    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        var stringValue = reader.GetString();
        if (stringValue != null && ToEnum.TryGetValue(stringValue, out var enumValue)) {
            return enumValue;
        }

        throw new JsonException($"Unable to convert '{stringValue}' to {typeof(TEnum).Name}.");
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options) {
        if (ToString.TryGetValue(value, out var stringValue)) {
            writer.WriteStringValue(stringValue);
        }
        else {
            writer.WriteStringValue(value.ToString());
        }
    }
}
