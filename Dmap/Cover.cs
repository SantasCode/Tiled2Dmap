using Tiled2Dmap.CLI.Utility;

namespace Tiled2Dmap.CLI.Dmap
{
    public class Cover
    {
        public string AniPath { get; set; }
        public string AniName { get; init; }
        public TilePosition Position { get; init; }
        /// <summary>
        /// Perhaps the number of tiles the cover object occupies on the ground, so a tree would be 1x1 or 2x2 while a wall might be 10x1.
        /// </summary>
        public Size BaseSize { get; init; }
        public PixelPosition Offset { get; init; }
        public uint AnimationInterval { get; init; }
    }
}
