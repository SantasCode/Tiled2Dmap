using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tiled2Dmap.CLI.Utility;

namespace Tiled2Dmap.CLI.Dmap
{
    public readonly struct EffectNew
    {
        public string EffectName { get; init; }
        public PixelPosition Position { get; init; }
        public uint Unk1_u32 { get; init; }
        public uint Unk2_u32 { get; init; }
        public uint Unk3_u32 { get; init; }
        public uint Unk4_u32 { get; init; }
        public uint Unk5_u32 { get; init; }
        public uint Unk6_u32 { get; init; }
        public uint Unk7_u32 { get; init; }

    }
}
