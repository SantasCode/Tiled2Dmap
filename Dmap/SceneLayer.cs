using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tiled2Dmap.CLI.Dmap
{
    public class SceneLayer
    {
        public uint Index { get; set; }
        public Utility.PixelPosition MoveRate { get; set; }
        public List<TerrainScene> TerrainScenes { get; set; } = new();
        public List<string> Puzzles { get; set; } =new();
        public List<Effect> Effects { get; set; } = new();
        public List<EffectNew> EffectNews { get; set; } = new();
    }
}
