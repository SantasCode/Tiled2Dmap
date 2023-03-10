using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tiled2Dmap.CLI.Dmap.Pux
{
    public readonly struct TextureGroup
    {
        public byte[] UnknownBytes { get; init; }//Simplified chinese string, appears unused in client.
        public string  AniFile { get; init; }
        public string AniName { get; init; }
        public uint unk1 { get; init; }
        public uint unk2 { get; init; }
        public uint unk3 { get; init; }
        public uint unk4 { get; init; }
        public uint Max { get; init; }
    }
}
