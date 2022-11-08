using BCnEncoder.Decoder;
using BCnEncoder.Encoder;
using BCnEncoder.ImageSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace Tiled2Dmap.CLI.ImageServices
{
    public class DDSConvert
    {
        private static BcDecoder _decoder = new BcDecoder();
        private static BcEncoder _encoder = new BcEncoder();
        public static Bitmap ToPng(string filePath)
        {
            using( FileStream fs = File.OpenRead(filePath))
            {
                return StreamToPng(fs);
            }
        }
        public static Bitmap StreamToPng(Stream fileStream)
        {
            using (Image<Rgba32> image = _decoder.DecodeToImageRgba32(fileStream))
            {
                using (var stream = new MemoryStream())
                {
                    var encoder = image.GetConfiguration().ImageFormatsManager.FindEncoder(PngFormat.Instance);
                    image.Save(stream, encoder);

                    stream.Seek(0, SeekOrigin.Begin);
                    return new System.Drawing.Bitmap(stream);
                }
            }
        }
        public static void PngToDDS(string ImagePath, string Output)
        {
            PngToDDS(new Bitmap(ImagePath), Output);
        }
        public static void PngToDDS(Bitmap Image, string Output)
        {
            _encoder.OutputOptions.GenerateMipMaps = false;
            _encoder.OutputOptions.FileFormat = BCnEncoder.Shared.OutputFileFormat.Dds;
            _encoder.OutputOptions.Format = BCnEncoder.Shared.CompressionFormat.Bc1;

            using Image<Rgba32> isImage = ToImageSharp(Image);
            using (FileStream fs = File.OpenWrite(Output))
            {
                Image<Rgba32> toEncode = isImage;
                if (IsAlphaImage(toEncode))
                    _encoder.OutputOptions.Format = BCnEncoder.Shared.CompressionFormat.Bc2;
                _encoder.EncodeToStream(isImage, fs);
            }
        }
        private static bool IsAlphaImage(Image<Rgba32> image)
        {
            for (int y = 0; y < image.Height; y++)
            {
                // It's faster to get the row and avoid a per-pixel multiplication using
                // the image[x, y] indexer
                Span<Rgba32> row = image.GetPixelRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    Rgba32 pixel = row[x];

                    if (pixel.A < byte.MaxValue)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        private static Image<Rgba32> ToImageSharp(Bitmap source)
        {
            using (var stream = new MemoryStream())
            {
                source.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Seek(0, SeekOrigin.Begin);
                return SixLabors.ImageSharp.Image.Load<Rgba32>(stream);
            }
        }
    }
}
