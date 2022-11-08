using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tiled2Dmap.CLI.Dmap
{
    public enum MapObjectType
    {
        Terrain = 0x01,
        MapScene = 0x03,
        Cover = 0x04,
        Puzzle = 0x08,
        Effect = 0x0A,
        Sound = 0x0F,
        EffectNew = 0x13,
        Unknown1 = 0x18,
        Unknown2 = 0x1b
    }
}
