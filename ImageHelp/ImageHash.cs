using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Drawing;

namespace Tiled2Dmap.CLI.ImageServices
{
    public static class ImageHash
    {
        public static string GetImageHash(Bitmap Image)
        {
            byte[] imgByte = (byte[])new ImageConverter().ConvertTo(Image, typeof(byte[]));

            SHA256Managed sha = new();
            byte[] hash = sha.ComputeHash(imgByte);
            return Convert.ToBase64String(hash);
        }
    }
}
