using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tiled2Dmap.CLI.ImageServices
{
    public static class ImageFont
    {
        public static void DrawBmpOnBmp(Bitmap src, Bitmap dst, int dstX, int dstY)
        {
            for (int x = 0; x < src.Width; x++)
                for (int y = 0; y < src.Height; y++)
                {
                    Color src_px = src.GetPixel(x, y);
                    dst.SetPixel(x+dstX, y+dstY, src_px);
                }
        }
        public static Size GetSize( int FixedLength)
        {
            return new Size((Resources.font_digit.Width / 10) * FixedLength + (FixedLength - 1), Resources.font_digit.Height);
        }
        public static Bitmap GetNumberBitmap(int Value, int FixedLength)
        {
            string Text = Value.ToString();
            int stringLen = Text.Length;

            if (stringLen > FixedLength)
                throw new ArgumentOutOfRangeException("Value provided is longer than the fixed length provided");

            //Determine starting index to center number on fixed width.
            int padding = FixedLength - stringLen;
            int padLeft = (padding / 2 + padding % 2) * (Resources.font_digit.Width / 10);


            Bitmap textBitmap = new Bitmap(GetSize( FixedLength).Width, GetSize( FixedLength).Height);//pixel font is monospace.
            for (int idx = 0, xoffset = padLeft; idx < stringLen; idx++, xoffset += (Resources.font_digit.Width / 10)+1)//Add one to xoffset to put single spacing between chars.
            {
                if (Text[idx] == '-')
                {
                    DrawBmpOnBmp(Resources.font_neg, textBitmap, xoffset, 0);
                }
                else
                {
                    int pixelFontOffset = (Text[idx] - 0x30) * (Resources.font_digit.Width / 10);
                    using (Bitmap charBmp = Resources.font_digit.Clone(new Rectangle(pixelFontOffset, 0, Resources.font_digit.Width / 10, Resources.font_digit.Height), Resources.font_digit.PixelFormat))
                    {
                        DrawBmpOnBmp(charBmp, textBitmap, xoffset, 0);
                    }
                }
            }

            return textBitmap;
        }
    }
}
