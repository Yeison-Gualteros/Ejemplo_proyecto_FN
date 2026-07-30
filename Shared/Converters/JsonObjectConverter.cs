// Shared/Converters/JsonObjectConverter.cs
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shared.Converters
{
    /// <summary>
    /// Convierte valores JSON a object?, preservando JsonElement para arrays y objetos anidados
    /// </summary>
    public class JsonObjectConverter : JsonConverter<object?>
    {
        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.True:
                    return true;
                case JsonTokenType.False:
                    return false;
                case JsonTokenType.Number:
                    if (reader.TryGetInt64(out long l))
                        return l;
                    return reader.GetDouble();
                case JsonTokenType.String:
                    return reader.GetString();
                case JsonTokenType.Null:
                    return null;
                case JsonTokenType.StartObject:
                    {
                        using var doc = JsonDocument.ParseValue(ref reader);
                        return doc.RootElement.Clone();
                    }
                case JsonTokenType.StartArray:
                    {
                        using var doc = JsonDocument.ParseValue(ref reader);
                        return doc.RootElement.Clone();
                    }
                default:
                    {
                        using var doc = JsonDocument.ParseValue(ref reader);
                        return doc.RootElement.Clone();
                    }
            }
        }

        public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            // ✅ Usar if/else en lugar de switch para evitar problemas con using
            if (value is bool b)
            {
                writer.WriteBooleanValue(b);
            }
            else if (value is string s)
            {
                writer.WriteStringValue(s);
            }
            else if (value is int i)
            {
                writer.WriteNumberValue(i);
            }
            else if (value is long l)
            {
                writer.WriteNumberValue(l);
            }
            else if (value is double d)
            {
                writer.WriteNumberValue(d);
            }
            else if (value is float f)
            {
                writer.WriteNumberValue(f);
            }
            else if (value is decimal dec)
            {
                writer.WriteNumberValue(dec);
            }
            else if (value is JsonElement je)
            {
                je.WriteTo(writer);
            }
            else
            {
                // ✅ Envolver en bloque para el using
                {
                    var json = JsonSerializer.Serialize(value, options);
                    using var doc = JsonDocument.Parse(json);
                    doc.RootElement.WriteTo(writer);
                }
            }
        }
    }
}