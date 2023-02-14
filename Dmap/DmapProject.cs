using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tiled2Dmap.CLI.Tiled;
using System.Text.Json;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Tiled2Dmap.CLI.ImageHelp;
using Tiled2Dmap.CLI.ImageServices;
using SixLabors.ImageSharp;

namespace Tiled2Dmap.CLI.Dmap
{
    public class DmapProject
    {
        private readonly ILogger _logger;

        private JsonSerializerOptions jsOptions;
        public string DmapDirectory { get { return Path.Combine(_projectDirectory, "dmap"); } }
        public string TiledDirectory { get { return Path.Combine(_projectDirectory, "tiled"); } }

        public DmapFile DmapFile { get; private set; }

        private readonly string _projectDirectory;
        private readonly string _projectName;
        private readonly string _mapName;
        public DmapProject(ILogger<DmapProject> logger, string projectDirectory, string mapName = "")
        {
            _logger = logger;

            _projectDirectory = projectDirectory;
            _projectName = new DirectoryInfo(projectDirectory).Name;

            if (mapName == "") 
                mapName = this._projectName;

            _mapName = mapName;

            #region Setup Json Options
            jsOptions = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = new Tiled.Json.LowerCaseNamingPolicy(),
                WriteIndented = true
            };
            jsOptions.Converters.Add(new Tiled.Json.TiledLayerConverter());
            jsOptions.Converters.Add(new Tiled.Json.TiledObjectConverter());
            jsOptions.Converters.Add(new Tiled.Json.TileConverter());
            #endregion Setup Json Options
        }

        /// <summary>
        /// Creates the DmapFile without copying assets.
        /// </summary>
        public void AssembleDmap()
        {
            #region Setup Json Options
            JsonSerializerOptions jsOptions = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = new Tiled.Json.LowerCaseNamingPolicy(),
                WriteIndented = true
            };
            jsOptions.Converters.Add(new Tiled.Json.TiledLayerConverter());
            jsOptions.Converters.Add(new Tiled.Json.TiledObjectConverter());
            jsOptions.Converters.Add(new Tiled.Json.TileConverter());
            jsOptions.Converters.Add(new Tiled.Json.TiledPropertyConverter());
            #endregion Setup Json Options

            DmapFile = new()
            {
                DmapPath = $"map/map/{_mapName}.dmap",
                Header = ASCIIEncoding.ASCII.GetBytes("CUSTOM01"),
                PuzzleFile = $"map/puzzle/{_mapName}.pul"
            };
            TiledMapFile accessMap = JsonSerializer.Deserialize<TiledMapFile>(File.ReadAllText(Path.Combine(TiledDirectory, "map_access.json")), jsOptions);
            DmapFile.SizeTiles = new((uint)accessMap.WidthTiles, (uint)accessMap.HeightTiles);
            DmapFile.TileSet = new Tile[DmapFile.SizeTiles.Width, DmapFile.SizeTiles.Height];

            TileLayer accessLayer = (TileLayer)accessMap.GetLayer("Access");
            TileLayer surfaceLayer = (TileLayer)accessMap.GetLayer("Surface");
            TileLayer heightLayer = (TileLayer)accessMap.GetLayer("Height");
            //Save tile information
            for (int tileX = 0; tileX < DmapFile.SizeTiles.Width; tileX++)
            {
                for (int tileY = 0; tileY < DmapFile.SizeTiles.Height; tileY++)
                {
                    int tileIdx = tileX + (tileY * (int)DmapFile.SizeTiles.Width);
                    DmapFile.TileSet[tileX, tileY] = new Tile
                    {
                        NoAccess = (ushort)((accessLayer.Data[tileIdx] - 1) == 0 ? 1 : 0), // Invert the 1 and 0..
                        Surface = (ushort)(int)accessMap.GetTile(TiledDirectory, surfaceLayer.Data[tileIdx], jsOptions).Properties.Where(p => p.Name == "Value").Select(p => p.Value).FirstOrDefault(),
                        Height = (short)(int)accessMap.GetTile(TiledDirectory, heightLayer.Data[tileIdx], jsOptions).Properties.Where(p => p.Name == "Value").Select(p => p.Value).FirstOrDefault()
                    };
                }
            }
            //Finished with accessMap.

            TiledMapFile mainMap = JsonSerializer.Deserialize<TiledMapFile>(File.ReadAllText(Path.Combine(TiledDirectory, "map_main.json")), jsOptions);
            ObjectLayer portalLayer = (ObjectLayer)mainMap.GetLayer("Portals");
            if(portalLayer != null)
            {
                foreach(var portal in portalLayer.Objects)
                {
                    if(portal.Class != "portal")
                    {
                        _logger.LogWarning("Invalid object type in the portal object group: {0}", portal.Class);
                        continue;
                    }
                    if(portal.Properties == null)
                    {
                        _logger.LogWarning("Portal without assigned Id");
                        continue;
                    }
                    TiledProperty portalProp = portal.Properties.Where(p => p.Name == "Id").FirstOrDefault();
                    if(portalProp == null)
                    {
                        _logger.LogWarning("Portal without assigned Id");
                        continue;
                    }
                    //int propId = (int)portalProp.Value;
                    DmapFile.Portals.Add(new()
                    {
                        Id = (uint)(int)portalProp.Value,
                        Position = new Utility.TilePosition()
                        {
                            X = (uint)portal.XPixels / (uint)Constants.DmapTileHeight,
                            Y = (uint)portal.YPixels / (uint)Constants.DmapTileHeight
                        }
                    }) ;
                }
            }
            else
                _logger.LogInformation("No Portal layer found");

            ObjectLayer effectLayer = (ObjectLayer)mainMap.GetLayer("Effects");
            if(effectLayer  != null)
            {
                foreach(var effect in effectLayer.Objects)
                {
                    if (effect.Class != "effect")
                    {
                        _logger.LogWarning("Invalid object type in the effect object group: {0}", effect.Class);
                        continue;
                    }
                    if (effect.Properties == null)
                    {
                        _logger.LogWarning("Effect without Effect property assigned.");
                        continue;
                    }
                    TiledProperty effectProp = effect.Properties.Where(p => p.Name == "Effect").FirstOrDefault();
                    if (effectProp == null)
                    {
                        _logger.LogWarning("Effect without Effect property assigned.");
                        continue;
                    }
                    DmapFile.Effects.Add(new()
                    {
                        EffectName = (string)effectProp.Value,
                        Position = new()
                        {
                            X = (int)(effect.XPixels - effect.YPixels),
                            Y = (int)(0.5 * (effect.YPixels + effect.XPixels + 32))
                        }
                    });
                }
            }
            else
                _logger.LogInformation("No Effect layer found");

            ObjectLayer soundLayer = (ObjectLayer)mainMap.GetLayer("Sounds");
            if (soundLayer != null)
            {
                foreach (var sound in soundLayer.Objects)
                {
                    if (sound.Class != "sound")
                    {
                        _logger.LogWarning($"Invalid object type in the sound object group {0}", sound.Class);
                        continue;
                    }
                    if (sound.Properties == null)
                    {
                        _logger.LogWarning("Portal without assigned Id");
                        continue;
                    }
                    TiledProperty soundProp = sound.Properties.Where(p => p.Name == "Sound").FirstOrDefault();
                    TiledProperty volumeProp = sound.Properties.Where(p => p.Name == "Volume").FirstOrDefault();
                    TiledProperty rangeProp = sound.Properties.Where(p => p.Name == "Range").FirstOrDefault();
                    if (soundProp == null || volumeProp == null || rangeProp == null)
                    {
                        _logger.LogWarning("Sound missing Sound, Volume, or Range property.");
                        continue;
                    }
                    DmapFile.Sounds.Add(new()
                    {
                        SoundFile = (string)soundProp.Value,
                        Volume = (uint)(int)volumeProp.Value,
                        Range = (uint)(int)rangeProp.Value,
                        Position = new()
                        {
                            X = (int)(sound.XPixels - sound.YPixels),
                            Y = (int)(0.5 * (sound.YPixels + sound.XPixels + 32))
                        }
                    });

                }
            }
            else
                _logger.LogInformation("No Sound layer found");

            ObjectLayer coverLayer = (ObjectLayer)mainMap.GetLayer("Covers");
            AniFile coverAniFile = new($"ani/{_mapName}c.ani");
            Dictionary<int, string> aniObjIdMap = new();
            int coverIdx = 0;
            if (coverLayer != null)
            {
                foreach (var cover in coverLayer.Objects)
                {
                    if (cover.Class != "cover")
                    {
                        _logger.LogWarning("Invalid object type in the cover object group: {0}", cover.Class);
                        continue;
                    }
                    if (cover.Properties == null)
                    {
                        _logger.LogWarning("Cover without additional properties");
                        continue;
                    }
                    TiledProperty baseWidthProp = cover.Properties.Where(p => p.Name == "BaseWidth").FirstOrDefault();
                    TiledProperty baseHeightProp = cover.Properties.Where(p => p.Name == "BaseHeight").FirstOrDefault();
                    if (baseWidthProp == null || baseHeightProp == null)
                    {
                        _logger.LogWarning("Cover missing BaseWidth or BaseHeight");
                        continue;
                    }

                    CoverObject coverObject = (cover as CoverObject);
                    string aniName = "";
                    int animationInterval = 0;
                    Utility.Size size = new();
                    if (!aniObjIdMap.TryGetValue(coverObject.GId, out aniName))
                    {
                        TiledTile coverTile = mainMap.GetTile(TiledDirectory, coverObject.GId, jsOptions);
                        InternalTileSet coverTileSet = mainMap.GetInternalTileSet("ts_cover.json");
                        Ani coverAni = new();
                        if (coverTile is AnimatedTile)
                        {
                            if (coverTile.Type != "animatedtile")
                            {
                                _logger.LogWarning("Invalid object type in the animated tile cover type: {0}", coverTile.Type);
                                continue;
                            }
                            int maxWidth = 0;
                            int maxHeight = 0;
                            foreach(var frame in (coverTile as AnimatedTile).Frames)
                            {
                                Tiled.Tile frameTile = (Tiled.Tile)mainMap.GetTile(TiledDirectory, frame.TileId+coverTileSet.FirstGId, jsOptions);
                                coverAni.Frames.Enqueue(frameTile.Image.Replace(".png", ".dds"));//Replace png with dds because we need to convert.
                                animationInterval = frame.Duration;
                                if (frameTile.ImageWidth > maxWidth) maxWidth = frameTile.ImageWidth;
                                if (frameTile.ImageHeight > maxHeight) maxHeight = frameTile.ImageHeight;

                            }
                            size = new()
                            {
                                Width = (uint)maxHeight,
                                Height = (uint)maxWidth
                            };
                        }
                        else if (coverTile is Tiled.Tile)
                        {
                            if (coverTile.Type != "tile")
                            {
                                _logger.LogWarning($"Invalid object type in the tile cover type: {0}", coverTile.Type);
                                continue;
                            }
                            coverAni.Frames.Enqueue((coverTile as Tiled.Tile).Image.Replace(".png", ".dds"));
                            size = new()
                            {
                                Width = (uint)(coverTile as Tiled.Tile).ImageWidth,
                                Height = (uint)(coverTile as Tiled.Tile).ImageHeight
                            };
                        }
                        else
                        {
                            _logger.LogWarning("Unrecogonized tile type in covers");
                            continue;
                        }
                        if (coverIdx == 267)
                            Log.Info("at 267");
                        coverAni.Name = $"Cover{coverIdx++}";
                        aniName = coverAni.Name;
                        aniObjIdMap.Add(coverObject.GId, coverAni.Name);
                        coverAniFile.Anis.Add(coverAni.Name, coverAni);
                    }
                    //Calculate Tile Position and Ortho Pixel Offset.
                    double tileXD = cover.XPixels / Constants.DmapTileHeight;
                    double tileYD = cover.YPixels / Constants.DmapTileHeight;
                    int tileX = (int)tileXD;
                    int tileY = (int)tileYD;
                    double isoOffsetX = cover.XPixels % Constants.DmapTileHeight;
                    double isoOffsetY = cover.YPixels % Constants.DmapTileHeight;

                    int orthoOffsetX = (int)(isoOffsetY - isoOffsetX);
                    int orthoOffsetY = (int)(0.5 * (isoOffsetY + isoOffsetX));



                    DmapFile.Covers.Add(new()
                    {
                        BaseSize = new()
                        {
                            Width = (uint)baseWidthProp.Value,
                            Height = (uint)baseHeightProp.Value

                        },
                        AnimationInterval = (uint)animationInterval,
                        AniPath = coverAniFile.AniFilePath,
                        AniName = aniName,
                        Position = new()
                        {
                            X = (uint)tileX,
                            Y = (uint)tileY
                        },
                        Offset = new()
                        {
                            X = orthoOffsetX,
                            Y = 16 - orthoOffsetY
                        }
                    });
                }
            }

            //TODO: Add Support for scenes and Scene Layers.

            //Copy over resources
            //TODO: Add support for 3d effects.
            if(DmapFile.Sounds.Count > 0)
            {
                Directory.CreateDirectory(Path.Combine(DmapDirectory, "sound"));
                foreach(var sound in DmapFile.Sounds)
                {
                    if(!File.Exists(Path.Combine(TiledDirectory, "sound", sound.SoundFile)))
                    {
                        _logger.LogWarning("Sound file doesn't exist {0}", Path.Combine(TiledDirectory, sound.SoundFile));
                        continue;
                    }
                    if(!File.Exists(Path.Combine(DmapDirectory, sound.SoundFile)))
                        File.Copy(Path.Combine(TiledDirectory,"sound", sound.SoundFile), Path.Combine(DmapDirectory, sound.SoundFile));
                }
            }
            if(DmapFile.Covers.Count > 0)
            {
                foreach(Ani ani in coverAniFile.Anis.Values)
                {
                    foreach(string coverpath in ani.Frames)
                    {
                        string tiledcoverpath = coverpath.Replace(".dds", ".png");
                        if (!File.Exists(Path.Combine(TiledDirectory, tiledcoverpath)))
                        {
                            _logger.LogWarning("Cover file doesn't exist {0}", Path.Combine(TiledDirectory, tiledcoverpath));
                            continue;
                        }

                        string coverfullPath = Path.Combine(DmapDirectory, coverpath);
                        string dirName = Path.GetDirectoryName(coverfullPath);

                        Directory.CreateDirectory(Path.GetDirectoryName(coverfullPath));

                        Image.Load<Rgba32>(Path.Combine(TiledDirectory, tiledcoverpath)).SaveAsDDS(File.OpenWrite(coverfullPath));
                    }    
                }
                //Save the ani
                coverAniFile.Save(Path.Combine(DmapDirectory, "ani"));
            }
            //Save the dmp
            DmapFile.Save(DmapDirectory);
        }

        private Rectangle GetPuzzleRect(Size mapSize, Size tileSize, int[] tileData)
        {
            int top = int.MaxValue, right = int.MinValue, bottom = int.MinValue, left = int.MaxValue;

            int mapPixelWidth = (mapSize.Width + mapSize.Height) * tileSize.Width / 2;
            int mapPixelHeight = (mapSize.Width + mapSize.Height) * tileSize.Height / 2;

            for(int yidx = 0; yidx < mapSize.Height; yidx++)
            {
                for(int xidx = 0; xidx < mapSize.Width; xidx++)
                {
                    //Skip it if the tile is undefined.
                    if (tileData[xidx + (yidx * mapSize.Width)] == 0) continue;

                    //Calculate the position of the center of this tile, top left is origin.
                    int tilePixelTop = (xidx + yidx) * tileSize.Height / 2;
                    int tilePixelBottom = tilePixelTop + tileSize.Height;
                    int tilePixelLeft = (mapPixelWidth / 2) + ((xidx - yidx) * tileSize.Width / 2);
                    int tilePixelRight = tilePixelLeft + tileSize.Width;

                    if(tilePixelTop < top) top = tilePixelTop;
                    if(tilePixelBottom > bottom) bottom = tilePixelBottom;
                    if(tilePixelLeft < left) left = tilePixelLeft;
                    if(tilePixelRight > right) right = tilePixelRight;
                }
            }

            //Calculate the size that is centered and captures the whole map.
            int rightDelta = right - mapPixelWidth / 2;
            int leftDelta = mapPixelWidth / 2 - left;
            int bottomDelta = bottom - mapPixelHeight / 2;
            int topDelta = mapPixelHeight / 2 - top;

            if (rightDelta > leftDelta)
                left = mapPixelWidth / 2 - rightDelta;
            else
                right = mapPixelWidth / 2 + leftDelta;

            if(bottomDelta > topDelta)
                top = mapPixelHeight / 2 - bottomDelta;
            else
                bottom = mapPixelHeight / 2 + topDelta;

            return new(left, top, right - left, bottom - top);
        }

        private Size GetPuzzleSizeAdder(Size mapSize, Size tileSize, int puzzlePieceSize)
        {
            //If it is already divisible by return 0,0
            if(IsEquallyDivisible(mapSize, puzzlePieceSize)) return new(0, 0);

            //check to see if its divisible by the puzzlePieceSize if we subtract a tileSize

            Size tempSize = mapSize - tileSize;

            if (IsEquallyDivisible(tempSize, puzzlePieceSize)) return tileSize * -1;

            //Determine how much larger it needs to be made to make it divisible.
            int deltaWidth = (puzzlePieceSize - (mapSize.Width % puzzlePieceSize)) % puzzlePieceSize;
            int deltaHeight = (puzzlePieceSize - (mapSize.Height % puzzlePieceSize)) % puzzlePieceSize;

            return new Size(deltaWidth, deltaHeight);
        }
        private bool IsEquallyDivisible(Size mapSize, int divisor)
        {
            return mapSize.Width % divisor == 0 && mapSize.Height % divisor == 0;
        }
        /// <summary>
        /// Creates Puzzle file and copies puzzle resources.
        /// </summary>
        public void AssemblePuzzle()
        {
            
            TiledMapFile mainMap = JsonSerializer.Deserialize<TiledMapFile>(File.ReadAllText(Path.Combine(TiledDirectory, "map_main.json")), jsOptions);

            TileLayer puzzleLayer = (TileLayer)mainMap.GetLayer("background");

            //Size of the isometric tiles.
            Size tileSize = new ();

            bool sizeSet = false;
            for(int yidx = 0; yidx < puzzleLayer.HeightTiles; yidx++)//Iterate isometric tile positions to find puzzle size.
            {
                for(int xidx = 0; xidx < puzzleLayer.WidthTiles; xidx++)
                {
                    int tileId = puzzleLayer.Data[xidx + (yidx * puzzleLayer.WidthTiles)];
                    if (tileId == 0) continue;//No tile set.

                    //Need to get the size of this tile to determine what the puzzle piece size is. (128 or 256). 
                    tileSize = mainMap.GetTileSize(TiledDirectory, tileId, jsOptions);

                    sizeSet = true;
                    break;
                }
                if (sizeSet)
                    break;
            }

            //Size of the resulting image. 
            Rectangle boundingRect = GetPuzzleRect(new Size(puzzleLayer.WidthTiles, puzzleLayer.HeightTiles), tileSize, puzzleLayer.Data);
            Size extendedBackgroundSize = boundingRect.Size;

            //Add an extended border around the background, of an additional tile.

            Point puzzleOffset = boundingRect.Location;

            #region Determine Size
            ///Need to determine the size of the background puzzle based on where the first defined tile is.
            ///Background puzzles don't always extend to the edges of the isometric bounds.
            #endregion Determine Size

            #region Isometric Stitch

            _logger.LogInformation("Stitching Background from isometric tiles");

            int expectedSlicedTiles = puzzleLayer.WidthTiles * puzzleLayer.HeightTiles;
            Utility.ProgressBar stitchProgress = new(expectedSlicedTiles, 10);

            _logger.LogInformation("Stitching Puzzle File...{0:000}%", stitchProgress.Progress);

            Stopwatch sw1 = Stopwatch.StartNew();

            using SixLabors.ImageSharp.Image<Rgba32> backgroundImageIS = new(extendedBackgroundSize.Width, extendedBackgroundSize.Height);


            for (int yidx = 0; yidx < puzzleLayer.HeightTiles; yidx++)//Iterate isometric tile positions.
            {
                for (int xidx = 0; xidx < puzzleLayer.WidthTiles; xidx++)
                {
                    int tileId = puzzleLayer.Data[xidx + (yidx * puzzleLayer.WidthTiles)];
                    if (tileId == 0) continue;//No tile set.

                    TiledTile tile = mainMap.GetTile(TiledDirectory, tileId, jsOptions);

                    if (tile.Type == "animatedtile")
                    {
                        _logger.LogError("Animated tiles not supported on background");
                        throw new Exception("Animated tiles not supported on backgrond");
                    }

                    string imgPath = (tile as Tiled.Tile).Image;

                    using var puzzlePiece = Image.Load<Rgba32>(Path.Combine(TiledDirectory, imgPath));

                    //(Iso X world origin + tile offset - tildwidth offset)
                    int wx = (puzzleLayer.WidthTiles * tileSize.Width / 2) + ((xidx - yidx) * tileSize.Width / 2) - tileSize.Width / 2;
                    int wy = (xidx + yidx) * tileSize.Height / 2;
                    int px = wx - puzzleOffset.X - tileSize.Width / 2; //World to Puzzle + offset for extended border.
                    int py = wy - puzzleOffset.Y;//World to Puzzle + offset for extended border.

                    backgroundImageIS.Mutate(x =>
                    {
                        x.DrawImage(puzzlePiece, new Point(px,py), 1);
                    });
                    
                    if (stitchProgress.Increment(1))
                        _logger.LogInformation("Stitching Puzzle File...{0:000}%", stitchProgress.Progress);
                }
            }

            sw1.Stop();

            _logger.LogInformation("Stitching Puzzle File completed in {0} seconds",sw1.Elapsed.TotalSeconds);
            #endregion Isometric Stitch

            backgroundImageIS.SaveAsPng("C:/Temp/comaps/stitches.png");

            //Slice all puzzle pieces to be 256. I don't see a reason to continue to support 128.
            int puzzleSize = 256;

            Size sizeAdder = GetPuzzleSizeAdder(new(backgroundImageIS.Width, backgroundImageIS.Height), tileSize, puzzleSize);
            //The image may need to be made smaller or larger depending on the sizeAdder.
            if(sizeAdder.Width < 0 || sizeAdder.Height < 0)
            {
                //Need to crop the image.
                backgroundImageIS.Mutate(x =>
                {
                    sizeAdder *= -1;
                    x.Crop(new(sizeAdder.Width / 2, sizeAdder.Height / 2, backgroundImageIS.Width - sizeAdder.Width, backgroundImageIS.Height - sizeAdder.Height));
                });
            }
            else if(sizeAdder.Width > 0 || sizeAdder.Height > 0)
            {
                backgroundImageIS.Mutate(x =>
                {
                    x.Pad(backgroundImageIS.Width + sizeAdder.Width, backgroundImageIS.Height + sizeAdder.Height);
                });
            }
            
            System.Drawing.Size puzzleTileSize = new(puzzleSize, puzzleSize);



            AniFile aniFile = new AniFile($"ani/{_mapName}.ani");
            PuzzleFile puzzleFile = new PuzzleFile($"map/puzzle/{_mapName}.pul")
            {
                Size = new()
                {
                    Width = (uint)(backgroundImageIS.Width / puzzleTileSize.Width),
                    Height = (uint)(backgroundImageIS.Height / puzzleTileSize.Height)
                },
                AniFile = aniFile.AniFilePath,
                Header = "PUZZLE2",
                RollSpeed = new() { X = 0, Y = 0}
            };
            puzzleFile.PuzzleTiles = new ushort[puzzleFile.Size.Width, puzzleFile.Size.Height];

            
            //Create the data directory for map DDS resources.
            string relativeResourcePath = Path.Combine("data", "map", "puzzle", _mapName);
            string ResourcePath = Path.Combine(DmapDirectory, relativeResourcePath);
            Directory.CreateDirectory(ResourcePath);

            _logger.LogInformation("Slicing background into puzzle pieces");

            expectedSlicedTiles = (int)puzzleFile.Size.Width * (int)puzzleFile.Size.Height;
            Utility.ProgressBar sliceProgress = new(expectedSlicedTiles, 10);

            using Image<Rgba32> puzzleSlice = new(puzzleTileSize.Width, puzzleTileSize.Height);
            
            _logger.LogInformation("Slicing Puzzle File...{0:000}%", sliceProgress.Progress);
            sw1 = Stopwatch.StartNew();

            int puzzleIdx = 0;
            Dictionary<string, int> imgHashMap = new();
            for(int yidx = 0; yidx < puzzleFile.Size.Height; yidx++)
            {
                for(int xidx = 0; xidx < puzzleFile.Size.Width; xidx++)
                {
                    backgroundImageIS.Extract(puzzleSlice, new(xidx * puzzleTileSize.Width, yidx * puzzleTileSize.Height));

                    string hash = ImageHash.GetImageHash(puzzleSlice);
                    int thisIdx = 0;
                    if(!imgHashMap.TryGetValue(hash, out thisIdx))
                    {
                        thisIdx = puzzleIdx++;
                        //imgHashMap.Add(hash, thisIdx);

                        Ani tmpAni = new();
                        tmpAni.Name = $"Puzzle{thisIdx}";
                        string relativeFramePath = Path.Combine(relativeResourcePath, $"{puzzleIdx}.dds"); 
                        tmpAni.Frames.Enqueue(relativeFramePath);

                        //Convert and save dds to anipath.
                        puzzleSlice.SaveAsDDS(File.OpenWrite(Path.Combine(DmapDirectory, relativeFramePath)));

                        aniFile.Anis.Add(tmpAni.Name, tmpAni);
                    }
                    puzzleFile.PuzzleTiles[xidx, yidx] = (ushort)thisIdx;
                    
                    if(sliceProgress.Increment(1))
                        _logger.LogInformation("Slicing Puzzle File...{0:000}%", sliceProgress.Progress);
                }
            }
            sw1.Stop();
            Console.WriteLine("Slicing Puzzle File completed in {0} seconds", sw1.Elapsed.TotalSeconds);

            //Save the puzzle and the ani files.
            puzzleFile.Save(DmapDirectory);
            aniFile.Save(DmapDirectory);
        }
    }
}


