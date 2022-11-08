using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace Tiled2Dmap.CLI.Tiled
{
    public abstract class TiledObject
    {
        [JsonPropertyName("x")]
        public double XPixels { get; set; }
        [JsonPropertyName("y")]
        public double YPixels { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Name { get; set; }
        public List<TiledProperty> Properties { get; set; }
        public abstract string Type { get; set; }
    }
    public class EffectObject : TiledObject
    {
        //Will this has any graphics
        public EffectObject() { }
        public EffectObject(string EffectPath)
        {
            this.Properties = new()
            {
                new ()
                {
                    Name = "Effect",
                    Type = "string",
                    Value = EffectPath
                }
            };
        }

        public override string Type { get; set; } = "effect";
    }
    public class PortalObject : TiledObject
    {
        public int GId { get; set; }
        public override string Type { get; set; } = "portal";
        public PortalObject() { }
        public PortalObject(uint PortalId)
        {
            Properties = new()
            {
                new()
                {
                    Name = "Id",
                    Value = PortalId,
                    Type = "int"
                }
            };
        }
    }
    public class SoundObject : TiledObject
    {
        public bool Ellipse { get; set; } = true;
        public SoundObject() { }
        public SoundObject(string SoundPath, int Volume, int Range)
        {
            this.Properties = new()
            {
                new()
                {
                    Name = "Sound",
                    Type = "string",
                    Value = SoundPath
                },
                new()
                {
                    Name = "Volume",
                    Type = "int",
                    Value = Volume
                },
                new()
                {
                    Name = "Range",
                    Type = "int",
                    Value = Range
                }
            };
        }

        public override string Type { get; set; } = "sound";
    }
    public class CoverObject : TiledObject
    {
        public int GId { get; set; }
        public override string Type { get; set; } = "cover";

        public CoverObject() { }
        public CoverObject(uint BaseWidth, uint BaseHeight)
        {
            this.Properties = new()
            {
                new()
                {
                    Name = "BaseWidth",
                    Type = "uint",
                    Value = BaseWidth
                },
                new()
                {
                    Name = "BaseHeight",
                    Type = "uint",
                    Value = BaseHeight
                }
            };
        }
    }
}


