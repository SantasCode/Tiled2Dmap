using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tiled2Dmap.CLI
{
    public static class Constants
    {
        public static int TiledTileWidth { get { return 256; } }
        public static int TiledTileHeight { get { return TiledTileWidth / 2; } }
        public static int DmapTileWidth { get { return 64; } }
        public static int DmapTileHeight { get { return DmapTileWidth / 2; } }
        public static int PuzzleWidth { get { return 256; } }
        public static int PuzzleHeight { get { return 256; } }
    }
}
