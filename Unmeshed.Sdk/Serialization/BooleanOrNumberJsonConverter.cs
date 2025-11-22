using System.Text.Json;
using System.Text.Json.Serialization;

namespace Unmeshed.Sdk.Serialization;

/// <summary>
/// JSON converter that handles boolean values that may be represented as numbers (0/1) or booleans (true/false).
/// </summary>
public class BooleanOrNumberJsonConverter : JsonConverter<bool>
{
    /// <summary>
    /// Reads and converts the JSON to a boolean.
    /// </summary>
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.True:
                return true;
            case JsonTokenType.False:
                return false;
            case JsonTokenType.Number:
                // Handle numeric representation: 0 = false, non-zero = true
                if (reader.TryGetInt32(out int intValue))
                {
                    return intValue != 0;
                }
                if (reader.TryGetInt64(out long longValue))
                {
                    return longValue != 0;
                }
                throw new JsonException($"Unable to convert number to boolean at position {reader.TokenStartIndex}");
            case JsonTokenType.String:
                // Handle string representation for robustness
                var stringValue = reader.GetString();
                if (bool.TryParse(stringValue, out bool boolValue))
                {
                    return boolValue;
                }
                if (int.TryParse(stringValue, out int numValue))
                {
                    return numValue != 0;
                }
                throw new JsonException($"Unable to convert string '{stringValue}' to boolean");
            default:
                throw new JsonException($"Unexpected token type {reader.TokenType} when parsing boolean");
        }
    }

    /// <summary>
    /// Writes a boolean value as JSON.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        writer.WriteBooleanValue(value);
    }
}
