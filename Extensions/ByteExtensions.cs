using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tiled2Dmap.CLI.Extensions
{
    public static class ByteExtensions
    {
        public static string GetString(this byte[] array)
        {
            if (array.Length == 0) return null;

            string value = "";
            foreach (byte b in array)
                value += string.Format("{0:X2}, ", b);

            return value.TrimEnd(' ', ',');
        }
    }
}
