using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tiled2Dmap.CLI.Tiled;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Drawing;
using System.Security.Cryptography;
using System.Diagnostics;

namespace Tiled2Dmap.CLI.Dmap
{
    public class DmapProject
    {
        public string DmapDirectory { get { return Path.Combine(ProjectDirectory, "dmap"); } }
        public string TiledDirectory { get { return Path.Combine(ProjectDirectory, "tiled"); } }

        public DmapFile DmapFile { get; private set; }

        private string ProjectDirectory;
        private string ProjectName;
        private string MapName;
        public DmapProject(string ProjectDirectory, string MapName = "")
        {
            this.ProjectDirectory = ProjectDirectory;
            this.ProjectName = new DirectoryInfo(ProjectDirectory).Name;
            if (MapName == "") 
                MapName = this.ProjectName;
            this.MapName = MapName;

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
                DmapPath = $"map/map/{MapName}.dmap",
                Header = ASCIIEncoding.ASCII.GetBytes("CUSTOM01"),
                PuzzleFile = $"map/puzzle/{MapName}.pul"
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
                    if(portal.Type != "portal")
                    {
                        Log.Warn($"Invalid object type in the portal object group: {portal.Type}");
                        continue;
                    }
                    if(portal.Properties == null)
                    {
                        Log.Warn("Portal without assigned Id");
                        continue;
                    }
                    TiledProperty portalProp = portal.Properties.Where(p => p.Name == "Id").FirstOrDefault();
                    if(portalProp == null)
                    {
                        Log.Warn("Portal without assigned Id");
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
                Log.Info("No Portal layer found");
            ObjectLayer effectLayer = (ObjectLayer)mainMap.GetLayer("Effects");
            if(effectLayer  != null)
            {
                foreach(var effect in effectLayer.Objects)
                {
                    if (effect.Type != "effect")
                    {
                        Log.Warn($"Invalid object type in the effect object group: {effect.Type}");
                        continue;
                    }
                    if (effect.Properties == null)
                    {
                        Log.Warn("Effect without Effect property assigned.");
                        continue;
                    }
                    TiledProperty effectProp = effect.Properties.Where(p => p.Name == "Effect").FirstOrDefault();
                    if (effectProp == null)
                    {
                        Log.Warn("Effect without Effect property assigned.");
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
                Log.Info("No Effect layer found");
            ObjectLayer soundLayer = (ObjectLayer)mainMap.GetLayer("Sounds");
            if (soundLayer != null)
            {
                foreach (var sound in soundLayer.Objects)
                {
                    if (sound.Type != "sound")
                    {
                        Log.Warn($"Invalid object type in the sound object group {sound.Type}");
                        continue;
                    }
                    if (sound.Properties == null)
                    {
                        Log.Warn("Portal without assigned Id");
                        continue;
                    }
                    TiledProperty soundProp = sound.Properties.Where(p => p.Name == "Sound").FirstOrDefault();
                    TiledProperty volumeProp = sound.Properties.Where(p => p.Name == "Volume").FirstOrDefault();
                    TiledProperty rangeProp = sound.Properties.Where(p => p.Name == "Range").FirstOrDefault();
                    if (soundProp == null || volumeProp == null || rangeProp == null)
                    {
                        Log.Warn("Sound missing Sound, Volume, or Range property.");
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
                Log.Info("No Sound layer found");

            ObjectLayer coverLayer = (ObjectLayer)mainMap.GetLayer("Covers");
            AniFile coverAniFile = new($"ani/{MapName}c.ani");
            Dictionary<int, string> aniObjIdMap = new();
            int coverIdx = 0;
            if (coverLayer != null)
            {
                foreach (var cover in coverLayer.Objects)
                {
                    if (cover.Type != "cover")
                    {
                        Log.Warn($"Invalid object type in the cover object group: {cover.Type}");
                        continue;
                    }
                    if (cover.Properties == null)
                    {
                        Log.Warn("Cover without additional properties");
                        continue;
                    }
                    TiledProperty baseWidthProp = cover.Properties.Where(p => p.Name == "BaseWidth").FirstOrDefault();
                    TiledProperty baseHeightProp = cover.Properties.Where(p => p.Name == "BaseHeight").FirstOrDefault();
                    if (baseWidthProp == null || baseHeightProp == null)
                    {
                        Log.Warn("Cover missing BaseWidth or BaseHeight");
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
                                Log.Warn($"Invalid object type in the animated tile cover type: {coverTile.Type}");
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
                                Log.Warn($"Invalid object type in the tile cover type: {coverTile.Type}");
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
                            Log.Warn("Unrecogonized tile type in covers");
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
                        Log.Warn($"Sound file doesn't exist {Path.Combine(TiledDirectory, sound.SoundFile)}");
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
                            Log.Warn($"Cover file doesn't exist {Path.Combine(TiledDirectory, tiledcoverpath)}");
                            continue;
                        }
                        string coverfullPath = Path.Combine(DmapDirectory, coverpath);
                        string dirName = Path.GetDirectoryName(coverfullPath);
                        Directory.CreateDirectory(Path.GetDirectoryName(coverfullPath));
                        ImageServices.DDSConvert.PngToDDS(Path.Combine(TiledDirectory, tiledcoverpath), coverfullPath);
                    }    
                }
                //Save the ani
                coverAniFile.Save(Path.Combine(DmapDirectory, "ani"));
            }
            //Save the dmp
            DmapFile.Save(DmapDirectory);
        }
        /// <summary>
        /// Creates Puzzle file and copies puzzle resources.
        /// </summary>
        public void AssemblePuzzle()
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
            #endregion Setup Json Options
            TiledMapFile mainMap = JsonSerializer.Deserialize<TiledMapFile>(File.ReadAllText(Path.Combine(TiledDirectory, "map_main.json")), jsOptions);
            TileLayer puzzleLayer = (TileLayer)mainMap.GetLayer("background");
            bool sizeSet = false;
            Size extendedBackgroundSize = new(0, 0);
            Utility.PixelOffset puzzleOffset = new(0, 0);
            for(int yidx = 0; yidx < puzzleLayer.HeightTiles; yidx++)//Iterate isometric tile positions to find puzzle size.
            {
                for(int xidx = 0; xidx < puzzleLayer.WidthTiles; xidx++)
                {
                    int tileId = puzzleLayer.Data[xidx + (yidx * puzzleLayer.WidthTiles)];
                    if (tileId == 0) continue;//No tile set.

                    //If the size hasn't been set this should be the first tile with graphics.
                    int top = (xidx) * Constants.TiledTileHeight / 2;
                    int left = (xidx) * Constants.TiledTileWidth;
                    int height = (mainMap.WidthTiles - (xidx)) * Constants.TiledTileHeight;
                    int width = (puzzleLayer.WidthTiles * Constants.TiledTileWidth) - (left * 2);

                    //Need to pad the outside of the bitmap to account for half tile overlap.
                    width += Constants.TiledTileWidth;
                    height += Constants.TiledTileHeight;
                    extendedBackgroundSize = new(width, height);
                    puzzleOffset = new(left, top);
                    sizeSet = true;
                    break;
                }
                if (sizeSet)
                    break;
            }

            Log.Info("Stitching Background from isometric tiles");
            int currentProgress = 0;
            Console.Write($"Stitching Puzzle File...{currentProgress:000}%");
            Stopwatch sw1 = Stopwatch.StartNew();
            int expectedSlicedTiles = puzzleLayer.WidthTiles * puzzleLayer.HeightTiles;

            using Bitmap backgroundBmp = new(extendedBackgroundSize.Width, extendedBackgroundSize.Height);
            using (Graphics graphic = Graphics.FromImage(backgroundBmp))
            {
                for (int yidx = 0; yidx < puzzleLayer.HeightTiles; yidx++)//Iterate isometric tile positions.
                {
                    for (int xidx = 0; xidx < puzzleLayer.WidthTiles; xidx++)
                    {
                        int tileId = puzzleLayer.Data[xidx + (yidx * puzzleLayer.WidthTiles)];
                        if (tileId == 0) continue;//No tile set.

                        TiledTile tile = mainMap.GetTile(TiledDirectory, tileId, jsOptions);
                        if (tile.Type == "animatedtile")
                        {
                            Log.Error("Animated tiles not supported on background");
                            throw new Exception("Animated tiles not supported on backgrond");
                        }

                        string imgPath = (tile as Tiled.Tile).Image;
                        using (Bitmap tileBmp = new Bitmap(Path.Combine(TiledDirectory, imgPath)))
                        {
                            //(Iso X world origin + tile offset - tildwidth offset)
                            int wx = (puzzleLayer.WidthTiles * Constants.TiledTileWidth / 2) + ((xidx - yidx) * Constants.TiledTileWidth / 2) - Constants.TiledTileWidth / 2;
                            int wy = (xidx + yidx) * Constants.TiledTileHeight / 2;
                            int px = wx - puzzleOffset.X + Constants.TiledTileWidth / 2; //World to Puzzle + offset for extended border.
                            int py = wy - puzzleOffset.Y + Constants.TiledTileHeight / 2;//World to Puzzle + offset for extended border.

                            graphic.DrawImage(tileBmp, new Point(px, py));
                        }
                        int progress = ((puzzleLayer.WidthTiles * yidx + xidx) * 100) / expectedSlicedTiles;
                        if (progress > currentProgress)
                        {
                            currentProgress = progress;
                            Console.Write($"\rStitching Puzzle File...{currentProgress:000}%");
                        }
                    }
                }
            }
            sw1.Stop();
            Console.WriteLine($"\rStitching Puzzle File...100% - {sw1.Elapsed.TotalSeconds} seconds");

            using Bitmap trimBmp = backgroundBmp.Clone(new Rectangle(Constants.TiledTileWidth / 2, Constants.TiledTileHeight / 2, backgroundBmp.Width - Constants.TiledTileWidth, backgroundBmp.Height - Constants.TiledTileHeight), backgroundBmp.PixelFormat);
            backgroundBmp.Dispose();
            if(trimBmp.Width % Constants.PuzzleWidth != 0 || trimBmp.Height % Constants.PuzzleHeight != 0)
            {
                //TODO: This can be determined pre-stitch
                Log.Error($"Resulting background is not equally divisible by Puzzle size {Constants.PuzzleHeight}");
                return;
            }

            AniFile aniFile = new AniFile($"ani/{MapName}.ani");
            PuzzleFile puzzleFile = new PuzzleFile($"map/puzzle/{MapName}.pul")
            {
                Size = new()
                {
                    Width = (uint)(trimBmp.Width / Constants.PuzzleWidth),
                    Height = (uint)(trimBmp.Height / Constants.PuzzleHeight)
                },
                AniFile = aniFile.AniFilePath,
                Header = "PUZZLE2",
                RollSpeed = new() { X = 0, Y = 0}
            };
            puzzleFile.PuzzleTiles = new ushort[puzzleFile.Size.Width, puzzleFile.Size.Height];

            
            //Create the data directory for map DDS resources.
            string relativeResourcePath = Path.Combine("data", "map", "puzzle", MapName);
            string ResourcePath = Path.Combine(DmapDirectory, relativeResourcePath);
            Directory.CreateDirectory(ResourcePath);

            Log.Info("Slicing background into puzzle pieces");
            currentProgress = 0;
            Console.Write($"Slicing Puzzle File...{currentProgress:000}%");
            sw1 = Stopwatch.StartNew();
            expectedSlicedTiles = (int)puzzleFile.Size.Width * (int)puzzleFile.Size.Height;

            int puzzleIdx = 0;
            Dictionary<string, int> imgHashMap = new();
            for(int yidx = 0; yidx < puzzleFile.Size.Height; yidx++)
            {
                for(int xidx = 0; xidx < puzzleFile.Size.Width; xidx++)
                {
                    using (Bitmap puzzBmp = trimBmp.Clone(new Rectangle(xidx * Constants.PuzzleWidth, yidx * Constants.PuzzleHeight, Constants.PuzzleWidth, Constants.PuzzleHeight), trimBmp.PixelFormat))
                    {
                        string hash = ImageServices.ImageHash.GetImageHash(puzzBmp);
                        int thisIdx = 0;
                        if(!imgHashMap.TryGetValue(hash, out thisIdx))
                        {
                            thisIdx = puzzleIdx++;
                            imgHashMap.Add(hash, thisIdx);

                            Ani tmpAni = new();
                            tmpAni.Name = $"Puzzle{thisIdx}";
                            string relativeFramePath = Path.Combine(relativeResourcePath, $"{puzzleIdx}.dds"); 
                            tmpAni.Frames.Enqueue(relativeFramePath);

                            //Convert and save dds to anipath.
                            ImageServices.DDSConvert.PngToDDS(puzzBmp, Path.Combine(DmapDirectory, relativeFramePath));
                            aniFile.Anis.Add(tmpAni.Name, tmpAni);
                        }
                        puzzleFile.PuzzleTiles[xidx, yidx] = (ushort)thisIdx;
                    }
                    int progress = (((int)puzzleFile.Size.Width * yidx + xidx) * 100) / expectedSlicedTiles;
                    if (progress > currentProgress)
                    {
                        currentProgress = progress;
                        Console.Write($"\rSlicing Puzzle File...{currentProgress:000}%");
                    }
                }
            }
            sw1.Stop();
            Console.WriteLine($"\rSlicing Puzzle File...100% - {sw1.Elapsed.TotalSeconds} seconds");

            //Save the puzzle and the ani files.
            puzzleFile.Save(DmapDirectory);
            aniFile.Save(Path.Combine(DmapDirectory, "ani"));
        }
    }
}


