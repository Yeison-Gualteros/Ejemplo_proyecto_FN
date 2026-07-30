using Shared.Converters;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shared.DataTransferObjects.Documentos
{
    public class BloqueContenidoDto
    {
        [JsonPropertyName("tipo")]
        public string Tipo { get; set; } = "texto";

        [JsonPropertyName("contenido")]
        public string? Contenido { get; set; }

        [JsonPropertyName("urlImagen")]
        public string? UrlImagen { get; set; }

        [JsonPropertyName("orden")]
        public int? Orden { get; set; }

        [JsonPropertyName("metadatos")]
        // ✅ Usar el convertidor personalizado
        [JsonConverter(typeof(JsonObjectDictionaryConverter))]
        public Dictionary<string, object?>? Metadatos { get; set; }
    }

    /// <summary>
    /// Convertidor para Dictionary<string, object?> que preserva JsonElement
    /// </summary>
    public class JsonObjectDictionaryConverter : JsonConverter<Dictionary<string, object?>?>
    {
        public override Dictionary<string, object?>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Se esperaba un objeto JSON");

            var dict = new Dictionary<string, object?>();
            var objectConverter = new JsonObjectConverter();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return dict;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException("Se esperaba una propiedad");

                var propertyName = reader.GetString();
                reader.Read();

                // Usar el convertidor de object para cada valor
                dict[ propertyName! ] = objectConverter.Read(ref reader, typeof(object), options);
            }

            return dict;
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<string, object?>? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            var objectConverter = new JsonObjectConverter();

            foreach (var kvp in value)
            {
                writer.WritePropertyName(kvp.Key);
                objectConverter.Write(writer, kvp.Value, options);
            }

            writer.WriteEndObject();
        }
    }
}