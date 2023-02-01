using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using Tiled2Dmap.CLI.Dmap;
using System.Drawing;
using ImageMagick;
using System.IO;

namespace Tiled2Dmap.CLI.Tiled
{

    public class TileSetFile
    {
        public string Name { get; set; }
        public int FirstGId { get; set; } = 1;
        public List<TiledTile> Tiles { get; set; } = new();
        public int TileCount { get { return Tiles.Count; } }
        public int TileWidth { get; set; }
        public int TileHeight { get; set; }
        public string Image { get; set; }
        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }
        public Utility.PixelOffset TileOffset { get; set; }
        public string ObjectAlignment { get; set; } = "center";

        public static TileSetFile TileSetFileTextImage(string Name, string OutputDirectory, Dictionary<int, int> Values)
        {
            int maxStrLength = 0;
            foreach(int key in Values.Keys) if (key.ToString().Length > maxStrLength) maxStrLength = key.ToString().Length;

            Size TextImgSize = ImageServices.ImageFont.GetSize(maxStrLength);

            int imgWidth = Values.Count >= 5 ? (5 * TextImgSize.Width) : (Values.Count * TextImgSize.Width);
            int imgHeight = ((Values.Count / 5) + 1) * TextImgSize.Height;
            string imgPath = $"{OutputDirectory}/{Name}.png";

            TileSetFile tileSetFile = new()
            {
                Name = $"ts_{Name}",
                ImageWidth = imgWidth,
                ImageHeight = imgHeight,
                Image = Path.GetRelativePath(OutputDirectory, imgPath),
                TileWidth = TextImgSize.Width,
                TileHeight = TextImgSize.Height
            };
            tileSetFile.TileOffset = new()
            {
                X = 25,
                Y = -12//Offset was determined using manual trial and error in tiled.
            };


            int bitmapIdx = 0;
            using (Bitmap heightBitmap = new Bitmap(imgWidth, imgHeight))
            {
               // using (Graphics graphics = Graphics.FromImage(heightBitmap))
              //  {
                    foreach (var value in Values.OrderBy(k => k.Key).Select(k => k))
                    {
                        //Add a tile entry w/ properties.
                        Tile tmpTile = new()
                        {
                            Id = bitmapIdx
                        };
                        tmpTile.Properties = new();
                        tmpTile.Properties.Add(new()
                        {
                            Name = "Value",
                            Value = value.Key,
                            Type = "int"
                        });
                        tileSetFile.Tiles.Add(tmpTile);

                        //Set the value to the bitmapIdx to reference in the layer data.
                        Values[value.Key] = bitmapIdx;

                        using Bitmap tmpBitmap = ImageServices.ImageFont.GetNumberBitmap(value.Key, maxStrLength);

                        //Draw it to the main image.
                        //graphics.DrawImage(tmpBitmap, (bitmapIdx % 5) * TextImgSize.Width, ((bitmapIdx / 5) * TextImgSize.Height));
                        ImageServices.ImageFont.DrawBmpOnBmp(tmpBitmap, heightBitmap, (bitmapIdx % 5) * TextImgSize.Width, ((bitmapIdx / 5) * TextImgSize.Height));

                        //Add tile to tile set.
                        bitmapIdx++;
                    }
                //}
                heightBitmap.Save(imgPath);
            }
            return tileSetFile;
        }
    }
}
