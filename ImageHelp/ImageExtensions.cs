using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System;

namespace Tiled2Dmap.CLI.ImageHelp
{
    internal static class ImageExtensions
    {
        internal static void SubImage(this Image<Rgba32> sourceImage, Image<Rgba32> destinationImage, Point location)
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
    }
}
