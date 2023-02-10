using BCnEncoder.Decoder;
using BCnEncoder.Encoder;
using BCnEncoder.ImageSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;

namespace Tiled2Dmap.CLI.ImageServices
{
    public class DDSConvert
    {
        private static BcDecoder _decoder = new BcDecoder();
        private static BcEncoder _encoder = new BcEncoder();
        public static Image<Rgba32> LoadImageSharp(Stream fileStream)
        {
            return _decoder.DecodeToImageRgba32(fileStream);
        }
        
        public static void SaveDds(Image<Rgba32> image, Stream fileStream)
        {
            _encoder.OutputOptions.GenerateMipMaps = false;
            _encoder.OutputOptions.FileFormat = BCnEncoder.Shared.OutputFileFormat.Dds;
            _encoder.OutputOptions.Format = BCnEncoder.Shared.CompressionFormat.Bc1;

            if (IsAlphaImage(image))
                _encoder.OutputOptions.Format = BCnEncoder.Shared.CompressionFormat.Bc2;

            _encoder.EncodeToStream(image, fileStream);
        }
        public static void PngToDDS(string imagePath, string output)
        {
            PngToDDS(Image.Load<Rgba32>(imagePath), output);
        }
        public static void PngToDDS(Image<Rgba32> image, string output)
        {
            _encoder.OutputOptions.GenerateMipMaps = false;
            _encoder.OutputOptions.FileFormat = BCnEncoder.Shared.OutputFileFormat.Dds;
            _encoder.OutputOptions.Format = BCnEncoder.Shared.CompressionFormat.Bc1;

            SaveDds(image, File.OpenWrite(output));
        }
        private static bool IsAlphaImage(Image<Rgba32> image)
        {
            bool isAlpha = false;

            image.ProcessPixelRows(p =>
            {
                for (int i = 0; i < image.Height; i++)
                {
                    Span<Rgba32> row = p.GetRowSpan(i);
                    for (int j = 0; j < row.Length; j++)
                    {
                        if (row[j].A < byte.MaxValue)
                        {
                            isAlpha = true; break;
                        }
                    }
                    if (isAlpha) break;
                }
            });
            return isAlpha;
        }

    }
}
