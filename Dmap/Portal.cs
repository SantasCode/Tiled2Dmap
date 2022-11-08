using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tiled2Dmap.CLI.Dmap
{
    public readonly struct Portal
    {
        public Utility.TilePosition Position { get; init; }
        public uint Id { get; init; }
        public Portal(Utility.TilePosition Position, uint Id)
        {
            this.Position = Position;
            this.Id = Id;
        }
    }
}
