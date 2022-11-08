using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace Tiled2Dmap.CLI.Tiled
{
    public abstract class TiledTile
    {
        public int Id { get; set; }
        public List<TiledProperty> Properties { get; set; }
        public abstract string Type { get; set; }
    }
    public class AnimatedTile : TiledTile
    {
        [JsonPropertyName("animation")]
        public List<TiledFrame> Frames { get; set; } = new();
        public override string Type { get; set; } = "animatedtile";
    }
    public class Tile : TiledTile
    {
        public string Image { get; set; }
        public int ImageHeight { get; set; }
        public int ImageWidth { get; set; }
        public override string Type { get; set; } = "tile";
    }
}
