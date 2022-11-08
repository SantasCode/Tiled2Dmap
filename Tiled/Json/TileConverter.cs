using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tiled2Dmap.CLI.Tiled.Json
{
    public class TileConverter : JsonConverter<TiledTile>
    {
        public override TiledTile Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (JsonDocument.TryParseValue(ref reader, out var doc))
            {
                if (doc.RootElement.TryGetProperty("type", out var type))
                {
                    var typeValue = type.GetString();
                    var rootElement = doc.RootElement.GetRawText();

                    return typeValue switch
                    {
                        "animatedtile" => JsonSerializer.Deserialize<AnimatedTile>(rootElement, options),
                        "tile" => JsonSerializer.Deserialize<Tile>(rootElement, options),
                        _ => throw new JsonException($"{typeValue} has not been mapped to a custom type yet!")
                    };
                }

                throw new JsonException("Failed to extract type property, it might be missing?");
            }

            throw new JsonException("Failed to parse JsonDocument");
        }

        public override void Write(Utf8JsonWriter writer, TiledTile value, JsonSerializerOptions options)
        {
            switch (value.Type)
            {
                case "animatedtile": JsonSerializer.Serialize(writer, value as AnimatedTile, typeof(AnimatedTile), options); break;
                case "tile": JsonSerializer.Serialize(writer, value as Tile, typeof(Tile), options); break;
                default: throw new JsonException($"{value.Type} has not been mapped to a custom type yet!");
            }
        }
    }
}
