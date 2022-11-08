using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tiled2Dmap.CLI.Utility;

namespace Tiled2Dmap.CLI.Dmap
{
    public class ScenePart
    {
        public string AniPath { get; set; }
        public string AniName { get; init; }
        public PixelOffset PixelLocation { get; init; }
        public uint Interval { get; init; }
        public Size Size { get; init; }
        public uint Thickness { get; init; }
        public TileOffset TileOffset { get; init; }
        public int OffsetElevation { get; init; }
        public SceneTile[,] Tiles { get; set; }
    }
}
