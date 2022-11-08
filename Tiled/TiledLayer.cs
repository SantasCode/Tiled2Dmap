using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tiled2Dmap.CLI.Tiled
{
    public abstract class TiledLayer
    {
        public string Name { get; set; }
        public bool Visible { get; set; } = true;
        public float Opacity { get; set; } = 1;

        public abstract string Type { get; }

    }
    public class TileLayer : TiledLayer
    {
        [JsonPropertyName("width")]
        public int WidthTiles { get; set; }
        [JsonPropertyName("height")]
        public int HeightTiles { get; set; }
        public int[] Data { get; set; }
        public override string Type { get { return "tilelayer"; } }

    }
    public class ObjectLayer : TiledLayer
    {
        public List<TiledObject> Objects { get; set; } = new();
        public override string Type { get { return "objectgroup"; } }
    }
}
