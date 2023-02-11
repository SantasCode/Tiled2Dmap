using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System;
using System.IO;
using BCnEncoder.Encoder;
using BCnEncoder.ImageSharp;

namespace Tiled2Dmap.CLI.ImageHelp
{
    internal static class ImageExtensions
    {
        internal static void Extract(this Image<Rgba32> sourceImage, Image<Rgba32> destinationImage, Point location)
        {
            sourceImage.ProcessPixelRows(destinationImage, (sourceAccessor, targetAccessor) =>
            {
                for (int i = 0; i < destinationImage.Height; i++)
                {
                    Span<Rgba32> sourceRow = sourceAccessor.GetRowSpan(location.Y + i);
                    Span<Rgba32> targetRow = targetAccessor.GetRowSpan(i);

                    sourceRow.Slice(location.X, destinationImage.Width).CopyTo(targetRow);
                }
            });

        }

        internal static void CopyTo(this Image<Rgba32> sourceImage, Rectangle sourceRect, Image<Rgba32> destinationImage, Point destinationPoint) 
        {
            sourceImage.ProcessPixelRows(destinationImage, (sourceAccessor, targetAccessor) =>
            {
                for(int i = 0; i < sourceRect.Height; i++)
                {
                    Span<Rgba32> sourceRow = sourceAccessor.GetRowSpan(sourceRect.Y + i);
                    Span<Rgba32> targetRow = targetAccessor.GetRowSpan(destinationPoint.Y + i);

                    sourceRow.Slice(sourceRect.X, sourceRect.Width).CopyTo(targetRow.Slice(destinationPoint.X, sourceRect.Width));
                }
            });
        }

        internal static bool IsAlphaImage(this Image<Rgba32> image)
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

        internal static void SaveAsDDS(this Image<Rgba32> image, Stream fileStream)
        {
            BcEncoder encoder = new();

            encoder.OutputOptions.GenerateMipMaps = false;
            encoder.OutputOptions.FileFormat = BCnEncoder.Shared.OutputFileFormat.Dds;
            encoder.OutputOptions.Format = BCnEncoder.Shared.CompressionFormat.Bc1;

            if (IsAlphaImage(image))
                encoder.OutputOptions.Format = BCnEncoder.Shared.CompressionFormat.Bc2;

            encoder.EncodeToStream(image, fileStream);
        }


    }
}
