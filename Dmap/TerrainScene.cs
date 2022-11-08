using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tiled2Dmap.CLI.Dmap
{
    public readonly struct TerrainScene
    {
        public string SceneFile { get; init; }
        public Utility.TilePosition Position { get; init; }
        public TerrainScene(string SceneFile, Utility.TilePosition Position)
        {
            this.SceneFile = SceneFile;
            this.Position = Position;
        }
    }
}
