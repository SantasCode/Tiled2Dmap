using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tiled2Dmap.CLI.Tiled.Json
{
    public class TiledPropertyConverter : JsonConverter<TiledProperty>
    {
        public override TiledProperty Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (JsonDocument.TryParseValue(ref reader, out var doc))
            {
                if (doc.RootElement.TryGetProperty("type", out var type) && doc.RootElement.TryGetProperty("value", out var value) && doc.RootElement.TryGetProperty("name", out var name))
                {
                    object parseVal = null;
                    switch(type.ToString())
                    {
                        case "int":
                            int ivalue = 0;
                            value.TryGetInt32(out ivalue);
                            parseVal = ivalue;
                            break;
                        case "string": parseVal = value.ToString(); break;
                        default:
                            Log.Error($"Unsupported type {type} in TiledPropertyConverter");
                            parseVal = value;
                            break;
                    }
                    return new()
                    {
                        Name = name.ToString(),
                        Type = type.ToString(),
                        Value = parseVal
                    };
                }
                throw new JsonException("Failed to extract type property, it might be missing?");
            }
            throw new JsonException("Failed to parse JsonDocument");
        }

        public override void Write(Utf8JsonWriter writer, TiledProperty value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value);
        }
    }
}
