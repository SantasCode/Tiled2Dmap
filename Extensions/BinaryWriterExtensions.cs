using System.IO;
using System.Text;
using Tiled2Dmap.CLI.Utility;

namespace Tiled2Dmap.CLI.Extensions
{
    public static class BinaryWriterExtensions
    {
        public static void WriteASCIIString(this BinaryWriter binaryWriter, string Value, int Length)
        {
            binaryWriter.Write(ASCIIEncoding.ASCII.GetBytes(Value.PadRight(Length, '\0')));
        }
        public static void Write(this BinaryWriter binaryWriter, PixelPosition Value)
        {
            binaryWriter.Write(Value.X);
            binaryWriter.Write(Value.Y);
        }
        public static void Write(this BinaryWriter binaryWriter, TilePosition Value)
        {
            binaryWriter.Write(Value.X);
            binaryWriter.Write(Value.Y);
        }
        public static void Write(this BinaryWriter binaryWriter, Size Value)
        {
            binaryWriter.Write(Value.Width);
            binaryWriter.Write(Value.Height);
        }
        public static void Write(this BinaryWriter binaryWriter, PixelOffset Value)
        {
            binaryWriter.Write(Value.X);
            binaryWriter.Write(Value.Y);
        }
        public static void Write(this BinaryWriter binaryWriter, TileOffset Value)
        {
            binaryWriter.Write(Value.X);
            binaryWriter.Write(Value.Y);
        }
    }
}
