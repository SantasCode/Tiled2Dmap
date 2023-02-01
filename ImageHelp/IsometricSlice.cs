using ImageMagick;
using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.IO;
using Tiled2Dmap.CLI.Dmap;
using Tiled2Dmap.CLI.Tiled;
using Tiled2Dmap.CLI.Utility;

namespace Tiled2Dmap.CLI.ImageHelp
{
    internal class IsometricSliceResult
    {
        public TileSetFile TileSetFile { get; set; }
        public TileLayer TileLayer { get; set; }
        public int TileWidth { get; set; }
        public int TileHeight { get; set; }
    }
    internal class IsometricSlice : IDisposable
    {
        private readonly ILogger _logger;
        private readonly string _name;
        private readonly string _projectDirectory;
        private readonly CordConverter _cordConverter;

        private readonly IMagickImage<byte> _tilemask;
        private readonly IMagickImage<byte> _puzzleBackground;

        public int TileWidth { get; set; }
        public int TileHeight { get; set; }
        public int TileCountWidth { get; set; }
        public int TileCountHeight { get; set; }

        public IsometricSlice (ILogger<IsometricSlice> logger, string name, string projectDirectory, Bitmap image, CordConverter coordConverter)
        {
            _logger = logger;
            _name = name;
            _projectDirectory = projectDirectory;
            _cordConverter= coordConverter;

            MagickImageFactory imageFactory = new();

            //Determine the size of tile needed. Some maps don't support using a 256x128 tile.
            if(image.Width % 256 == 0 && image.Height % 256 == 0)
            {
                _tilemask = imageFactory.Create(Resources.diamond_256x128);
                TileWidth = 256;
                TileHeight = 128;
            }
            else if(image.Width % 128 == 0 && image.Height % 128 == 0)
            {
                _tilemask = imageFactory.Create(Resources.diamond_128x64);
                TileWidth = 128;
                TileHeight = 64;

            }
            else
            {
                _logger.LogError("Unable to determine puzzle size for background image size: {0}, {1}", image.Width, image.Height);
                throw new NotSupportedException(string.Format("Unable to determine puzzle size for background image size: {0}, {1}", image.Width, image.Height));
            }

            _logger.LogDebug("Isometric tile size is: {0}, {1}", TileWidth, TileHeight);

            _puzzleBackground = imageFactory.Create(image);

            //Determine the size of the tile map, based on the tile size.
            TileCountWidth = _cordConverter.dmapSize.Width / (TileHeight / Constants.DmapTileHeight);
            TileCountHeight = _cordConverter.dmapSize.Height / (TileWidth / Constants.DmapTileWidth);
            _logger.LogDebug("Number of tiles is: {0}, {1}", TileCountWidth, TileCountHeight);
        }

        public IsometricSliceResult Slice()
        {
            IsometricSliceResult result = new()
            {
                TileSetFile = new TileSetFile()
                {
                    Name = $"ts_{_name}",
                    FirstGId = 1,
                    TileWidth = TileWidth,
                    TileHeight = TileHeight
                },
                TileLayer = new TileLayer()
                {
                    Name = _name,
                    WidthTiles = TileCountWidth,
                    HeightTiles = TileCountHeight
                },
                TileWidth= TileWidth,
                TileHeight= TileHeight
            };

            result.TileLayer.Data = new int[TileCountWidth * TileCountHeight];

            //Define the bounds of the actual background before extending.
            Rectangle backgroundRect = new Rectangle(_tilemask.Width / 2, _tilemask.Height, _puzzleBackground.Width, _puzzleBackground.Height);

            //Add padding around the whole background image so we can slice partial tiles.
            _puzzleBackground.Extent(_puzzleBackground.Width + _tilemask.Width, _puzzleBackground.Height + _tilemask.Height, Gravity.Center, MagickColors.Transparent);

            //Setup total tracking.
            int expectedPieces = TileCountHeight * TileCountWidth;
            ProgressBar progress = new(expectedPieces, 10);

            _logger.LogInformation("Slicing image...{0:000}%", 0);

            int tileIdx = 0;

            for (int xidx = 0, tiledX = 0; xidx < _cordConverter.dmapSize.Width; xidx += TileWidth / Constants.DmapTileWidth, tiledX++)
            {
                for (int yidx = 0, tiledY = 0; yidx < _cordConverter.dmapSize.Width; yidx += TileHeight / Constants.DmapTileHeight, tiledY++)
                {
                    //Get the center of the dmap cell
                    Point cellCenter = _cordConverter.Cell2Bg(new Point(xidx, yidx));

                    Rectangle subImageRect = new Rectangle(cellCenter, new System.Drawing.Size(_tilemask.Width, _tilemask.Height));

                    //Adjust location for larger tile ---can't remember why I'm doing this...
                    subImageRect.Y += (TileHeight / 2) - (Constants.DmapTileHeight / 2);

                    //We only care about this subimage is it covers the background image.
                    if (subImageRect.IntersectsWith(backgroundRect))
                    {
                        using(var tileImage = _puzzleBackground.Clone(subImageRect.X, subImageRect.Y, subImageRect.Width, subImageRect.Height))
                        {
                            tileImage.Composite(_tilemask, CompositeOperator.DstIn);

                            string imgSavePath = $"{Path.Combine(_projectDirectory, "tiled", _name)}/img{tileIdx}.png";
                            string imgRelativePath = Path.GetRelativePath(Path.Combine(_projectDirectory, "tiled"), imgSavePath);

                            result.TileSetFile.Tiles.Add(new Tiled.Tile()
                            {
                                Id = tileIdx,
                                ImageWidth = Constants.TiledTileWidth,
                                ImageHeight = Constants.TiledTileHeight,
                                Image = imgRelativePath
                            });

                            result.TileLayer.Data[tiledX + (tiledY * result.TileLayer.WidthTiles)] = tileIdx + result.TileSetFile.FirstGId;

                            tileIdx++;

                            tileImage.Write(imgSavePath);
                        }
                    }

                    if (progress.Increment(1))
                        _logger.LogInformation("Slicing image...{0:000}%", progress.Progress);
                    
                }
            }

            _logger.LogInformation("Finished slicing. Total Unique tiles {0} of {1}", result.TileSetFile.TileCount, expectedPieces);

            return result;
        }
        public void Dispose()
        {
            if(_tilemask != null) { _tilemask.Dispose(); }
            if(_puzzleBackground != null) { _puzzleBackground.Dispose(); }
        }
    }
}
