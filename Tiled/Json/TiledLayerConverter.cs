using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tiled2Dmap.CLI.Tiled.Json
{
    public class TiledLayerConverter : JsonConverter<TiledLayer>
    {
        public override TiledLayer Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (JsonDocument.TryParseValue(ref reader, out var doc))
            {
                if (doc.RootElement.TryGetProperty("type", out var type))
                {
                    var typeValue = type.GetString();
                    var rootElement = doc.RootElement.GetRawText();

                    return typeValue switch
                    {
                        "tilelayer" => JsonSerializer.Deserialize<TileLayer>(rootElement, options),
                        "objectgroup" => JsonSerializer.Deserialize<ObjectLayer>(rootElement, options),
                        _ => throw new JsonException($"{typeValue} has not been mapped to a custom type yet!")
                    };
                }

                throw new JsonException("Failed to extract type property, it might be missing?");
            }

            throw new JsonException("Failed to parse JsonDocument");
        }

        public override void Write(Utf8JsonWriter writer, TiledLayer value, JsonSerializerOptions options)
        {
            switch (value.Type)
            {
                case "tilelayer": JsonSerializer.Serialize(writer, value as TileLayer, typeof(TileLayer), options); break;
                case "objectgroup": JsonSerializer.Serialize(writer, value as ObjectLayer, typeof(ObjectLayer), options); break;
                default: throw new JsonException($"{value.Type} has not been mapped to a custom type yet!");
            }
        }
    }
}
