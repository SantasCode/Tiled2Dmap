using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tiled2Dmap.CLI.Utility;

namespace Tiled2Dmap.CLI.Extensions
{
    public static class BinaryReaderExtensions
    {
        public static string ReadASCIIString(this BinaryReader binaryReader, int Length)
        {
            string result = ASCIIEncoding.ASCII.GetString(binaryReader.ReadBytes(Length));
            int index = result.IndexOf('\0');
            if (index < 0)
                return result;
            return result.Substring(0, index);
        }
        public static PixelPosition ReadPixelPosition(this BinaryReader binaryReader)
        {
            return new PixelPosition()
            {
                X = binaryReader.ReadInt32(),
                Y = binaryReader.ReadInt32()
            };
        }
        public static TilePosition ReadTilePosition(this BinaryReader binaryReader)
        {
            return new TilePosition()
            {
                X = binaryReader.ReadUInt32(),
                Y = binaryReader.ReadUInt32()
            };
        }
        public static Size ReadSize(this BinaryReader binaryReader)
        {
            return new Size()
            {
                Width = binaryReader.ReadUInt32(),
                Height = binaryReader.ReadUInt32()
            };
        }
        public static PixelOffset ReadPixelOffset(this BinaryReader binaryReader)
        {
            return new PixelOffset()
            {
                X = binaryReader.ReadInt32(),
                Y = binaryReader.ReadInt32()
            };
        }
        public static TileOffset ReadTileOffset(this BinaryReader binaryReader)
        {
            return new TileOffset()
            {
                X = binaryReader.ReadInt32(),
                Y = binaryReader.ReadInt32()
            };
        }
    }
}
