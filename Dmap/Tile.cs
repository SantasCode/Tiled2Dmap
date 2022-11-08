using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace Tiled2Dmap.CLI.Dmap
{
    public readonly struct Tile
    {
        [JsonIgnore]
        public ushort Access
        {
            get
            {
                if (NoAccess == 1)
                    return 0;
                else
                    return 1;
            }
        }
        public ushort NoAccess { get; init; }
        public ushort Surface { get; init; }
        public short Height { get; init; }

        public Tile(ushort NoAccess, ushort Surface, short Height)
        {
            this.NoAccess = NoAccess;
            this.Surface = Surface;
            this.Height = Height;
        }
    }
}
