using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tiled2Dmap.CLI.Utility
{
    public readonly struct PixelOffset
    {
        public int X { get; init; }
        public int Y { get; init; }
        public PixelOffset(int X, int Y)
        {
            this.X = X;
            this.Y = Y;
        }
    }
}
