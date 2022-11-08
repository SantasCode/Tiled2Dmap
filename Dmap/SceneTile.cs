using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tiled2Dmap.CLI.Dmap
{
    public readonly struct SceneTile
    {
        public uint Access
        {
            get
            {
                if (NoAccess == 1)
                    return 0;
                else
                    return 1;
            }
        }
        public uint NoAccess { get; init; }
        public uint Surface { get; init; }
        public int Height { get; init; }

        public SceneTile(uint NoAccess, uint Surface, int Height)
        {
            this.NoAccess = NoAccess;
            this.Surface = Surface;
            this.Height = Height;
        }
    }
}
