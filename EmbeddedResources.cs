using Microsoft.Extensions.FileProviders;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Reflection;

namespace Tiled2Dmap.CLI
{
    internal static class EmbeddedResources
    {
        private static Image<Rgba32> getImage(string imageName)
        {
            var provider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly());
            
            var file = provider.GetFileInfo(imageName);

            return Image.Load<Rgba32>(file.CreateReadStream());
        }

        private static Image<Rgba32> access = null;
        internal static Image<Rgba32> Access()
        {
            if (access == null)
                access = getImage("Resources/access.png");
            return access.Clone();
        }

        private static Image<Rgba32> diamond128 = null;
        internal static Image<Rgba32> Diamond128()
        {
            if (diamond128 == null)
                diamond128 = getImage("Resources/diamond_128x64.png");
            return diamond128.Clone();
        }

        private static Image<Rgba32> diamond256 = null;
        internal static Image<Rgba32> Diamond256()
        {
            if (diamond256 == null)
                diamond256 = getImage("Resources/diamond_256x128.png");
            return diamond256.Clone();
        }

        private static Image<Rgba32> portal128 = null;
        internal static Image<Rgba32> Portal128()
        {
            if (portal128 == null)
                portal128 = getImage("Resources/exit_128x64.png");
            return portal128.Clone();
        }

        private static Image<Rgba32> fontNegative = null;
        internal static Image<Rgba32> FontNegative()
        {
            if (fontNegative == null)
                fontNegative = getImage("Resources/font_neg.png");
            return fontNegative.Clone();
        }

        private static Image<Rgba32> fontNumbers = null;
        internal static Image<Rgba32> FontNumbers()
        {
            if (fontNumbers == null)
                fontNumbers = getImage("Resources/pixel_font.png");
            return fontNumbers.Clone();
        }
    }
}
