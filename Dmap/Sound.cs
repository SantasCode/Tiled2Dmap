using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tiled2Dmap.CLI.Dmap
{
    public readonly struct Sound
    {
        public string SoundFile { get; init; }
        public Utility.PixelPosition Position { get; init; }
        public uint Volume { get; init; }
        public uint Range { get; init; }

    }
}
