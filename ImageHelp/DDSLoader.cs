using System.IO;
using BCnEncoder.Decoder;
using BCnEncoder.ImageSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Tiled2Dmap.CLI.ImageServices
{
    public class DDSConvert
    {
        public static Image<Rgba32> Load(Stream fileStream)
        {
            var decoder = new BcDecoder();
            return decoder.DecodeToImageRgba32(fileStream);
        }
    }
}
