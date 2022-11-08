using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tiled2Dmap.CLI.Utility
{
    public readonly struct Size
    {
        public uint Width { get; init; }
        public uint Height { get; init; }

        public Size(uint Width, uint Height)
        {
            this.Width = Width;
            this.Height = Height;
        }
    }
}
