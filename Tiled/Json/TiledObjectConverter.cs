using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tiled2Dmap.CLI.Tiled.Json
{
    public class TiledObjectConverter : JsonConverter<TiledObject>
    {
        public override TiledObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (JsonDocument.TryParseValue(ref reader, out var doc))
            {
                if (doc.RootElement.TryGetProperty("class", out var type))
                {
                    var typeValue = type.GetString();
                    var rootElement = doc.RootElement.GetRawText();

                    return typeValue switch
                    {
                        "sound" => JsonSerializer.Deserialize<SoundObject>(rootElement, options),
                        "effect" => JsonSerializer.Deserialize<EffectObject>(rootElement, options),
                        "cover" => JsonSerializer.Deserialize<CoverObject>(rootElement, options),
                        "portal" => JsonSerializer.Deserialize<PortalObject>(rootElement, options),
                        _ => throw new JsonException($"{typeValue} has not been mapped to a custom type yet!")
                    };
                }

                throw new JsonException("Failed to extract type property, it might be missing?");
            }

            throw new JsonException("Failed to parse JsonDocument");
        }

        public override void Write(Utf8JsonWriter writer, TiledObject value, JsonSerializerOptions options)
        {
            switch (value.Class)
            {
                case "sound": JsonSerializer.Serialize(writer, value as SoundObject, typeof(SoundObject), options); break;
                case "effect": JsonSerializer.Serialize(writer, value as EffectObject, typeof(EffectObject), options); break;
                case "cover": JsonSerializer.Serialize(writer, value as CoverObject, typeof(CoverObject), options); break;
                case "portal": JsonSerializer.Serialize(writer, value as PortalObject, typeof(PortalObject), options); break;
                default: throw new JsonException($"{value.Class} has not been mapped to a custom type yet!");
            }
        }
    }
}
