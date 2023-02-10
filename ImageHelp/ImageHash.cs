using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Drawing;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;
using SixLabors.ImageSharp;

namespace Tiled2Dmap.CLI.ImageServices
{
    public static class ImageHash
    {

        public static string GetImageHash(SixLabors.ImageSharp.Image<Rgba32> image)
        {
            using SHA256Managed sha = new();

            using MemoryStream ms = new();

            image.SaveAsPng(ms);
            
            byte[] hash = sha.ComputeHash(ms);
            
            return Convert.ToBase64String(hash);
        }
    }
}
