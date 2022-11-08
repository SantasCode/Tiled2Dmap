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

        public static (TileSetFile, TileLayer) TileSetFromPuzzleFile(string Name, Utility.ClientResources ClientResources, string ProjectDirectory, string PuzzleFilePath, Utility.Size DmapCellSize)
        {
            PuzzleFile puzzleFile = new PuzzleFile(ClientResources.ClientDirectory, PuzzleFilePath);

            //Create one large 
            using ImageServices.Stitch imageStitch = new ImageServices.Stitch(ClientResources, puzzleFile);

            return TileSetFromImage(Name, imageStitch.Image, ProjectDirectory, new CoordConverter(new Size((int)DmapCellSize.Width, (int)DmapCellSize.Height), imageStitch.Image.Size));
        }

        public static (TileSetFile, TileLayer) TileSetFromImage(string Name, Bitmap PuzzleImage, string ProjectDirectory, CoordConverter CordinateConverter)
        {
            Console.WriteLine($"PuzzleImage Size = {PuzzleImage.Size.Width}, {PuzzleImage.Size.Height}");

            TileSetFile tileSet = new()
            {
                Name = $"ts_{Name}",
                FirstGId = 1,
                TileWidth = Constants.TiledTileWidth,
                TileHeight = Constants.TiledTileHeight
            };
            TileLayer tileLayer = new()
            {
                Name = Name,
                WidthTiles = CordinateConverter.dmapSize.Width / (Constants.TiledTileHeight / Constants.DmapTileHeight),
                HeightTiles = CordinateConverter.dmapSize.Height / (Constants.TiledTileWidth / Constants.DmapTileWidth)
            };
            tileLayer.Data = new int[tileLayer.WidthTiles * tileLayer.HeightTiles];

            MagickImageFactory magickImageFactory = new();
            using var tileMask = magickImageFactory.Create(Resources.diamond_256x128);

            using var mgkBackground = magickImageFactory.Create(PuzzleImage);

            //Add border around the background to we can slice partial edge pieces.
            Console.WriteLine("Extending border...");
            mgkBackground.Extent(mgkBackground.Width + tileMask.Width, mgkBackground.Height + tileMask.Height, Gravity.Center, MagickColors.Transparent);
            //Define the bounds of the actual background image, without border.
            Rectangle backgroundRectangle = new Rectangle(Constants.TiledTileWidth / 2, Constants.TiledTileHeight / 2, PuzzleImage.Size.Width, PuzzleImage.Size.Height);

            int currentProgress = 0;
            Console.Write($"Slicing Puzzle File...{currentProgress:000}%");
            int expectedSlicedTiles = tileLayer.WidthTiles * tileLayer.HeightTiles;

            //Id for tilest index.
            int tileIdx = 0;

            //Slice the background.
            for (int xidx = 0, tiledX = 0; xidx < CordinateConverter.dmapSize.Width; xidx += Constants.TiledTileWidth / Constants.DmapTileWidth, tiledX++)
            {
                for (int yidx = 0, tiledY = 0; yidx < CordinateConverter.dmapSize.Width; yidx += Constants.TiledTileHeight / Constants.DmapTileHeight, tiledY++)
                {
                    //This returns the center of the 64x32 tile
                    Point cellCenter = CordinateConverter.Cell2Bg(new Point(xidx, yidx));
                    Rectangle tiletoGetRect = new Rectangle(cellCenter, new Size(tileMask.Width, tileMask.Height));

                    //Adjust location for larger tile
                    tiletoGetRect.Y += (Constants.TiledTileHeight / 2) - (Constants.DmapTileHeight / 2);

                    if (tiletoGetRect.IntersectsWith(backgroundRectangle))
                    {
                        using (var mgkTileImage = mgkBackground.Clone(tiletoGetRect.X, tiletoGetRect.Y, tiletoGetRect.Width, tiletoGetRect.Height))
                        {
                            mgkTileImage.Composite(tileMask, CompositeOperator.DstIn);

                            string imgSavePath = $"{Path.Combine(ProjectDirectory, "tiled", Name)}/img{tileIdx}.png";
                            string imgRelativePath = Path.GetRelativePath(Path.Combine(ProjectDirectory, "tiled"), imgSavePath);

                            tileSet.Tiles.Add(new Tile()
                            {
                                Id = tileIdx,
                                ImageWidth = Constants.TiledTileWidth,
                                ImageHeight = Constants.TiledTileHeight,
                                Image = imgRelativePath
                            });

                            tileLayer.Data[tiledX + (tiledY * tileLayer.WidthTiles)] = tileIdx + tileSet.FirstGId;

                            tileIdx++;

                            //Save the image piece.
                            mgkTileImage.Write(imgSavePath);
                        }
                    }
                    int progress = ((tileLayer.WidthTiles * tiledX + tiledY) * 100) / expectedSlicedTiles;
                    if (progress > currentProgress)
                    {
                        currentProgress = progress;
                        Console.Write($"\rSlicing Puzzle File...{currentProgress:000}%");
                    }
                }
            }
            Console.WriteLine($"\rSlicing Puzzle File...100%");
            Console.WriteLine($"Finished slicing puzzle file. Total unique tiles {tileSet.TileCount} out of {expectedSlicedTiles}");
            return (tileSet, tileLayer);
        }

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
