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
using Tiled2Dmap.CLI.ImageServices;
using SixLabors.ImageSharp;

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
            var numbers = Values.Keys.ToList();
            numbers.Sort();

            ImageFontIS imageFontIS = new(EmbeddedResources.FontNumbers(), EmbeddedResources.FontNegative(), numbers);


            var numberImage = imageFontIS.GetNumbersImage();

            string imgPath = Path.Combine(OutputDirectory, $"{Name}.png");

            numberImage.SaveAsPng(imgPath);

            TileSetFile tileSetFile = new()
            {
                Name = $"ts_{Name}",
                ImageWidth = numberImage.Width,
                ImageHeight = numberImage.Height,
                Image = Path.GetRelativePath(OutputDirectory, imgPath),
                TileWidth = imageFontIS.NumberSize.Width,
                TileHeight = imageFontIS.NumberSize.Height
            };
            tileSetFile.TileOffset = new()
            {
                X = 25,
                Y = -12//Offset was determined using manual trial and error in tiled.
            };


            int bitmapIdx = 0;
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
                //Add tile to tile set.
                tileSetFile.Tiles.Add(tmpTile);

                //Set the value to the bitmapIdx to reference in the layer data.
                Values[value.Key] = bitmapIdx;

                //Increase the tileset idx.
                bitmapIdx++;
            }
            return tileSetFile;
        }
    }
}
