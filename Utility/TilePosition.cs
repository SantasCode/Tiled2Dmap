using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tiled2Dmap.CLI.Utility
{
    public readonly struct TilePosition
    {
        public uint X { get; init; }
        public uint Y { get; init; }
        public TilePosition (uint X, uint Y)
        {
            this.X = X;
            this.Y = Y;
        }
    }
}
