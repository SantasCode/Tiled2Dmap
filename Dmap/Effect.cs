using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tiled2Dmap.CLI.Dmap
{
    public readonly struct Effect
    {
        public string EffectName { get; init; }
        public Utility.PixelPosition Position { get; init; }
    }
}
