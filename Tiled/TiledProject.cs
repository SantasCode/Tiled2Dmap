using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Drawing;
using Tiled2Dmap.CLI.Dmap;
using Tiled2Dmap.CLI.ImageHelp;

namespace Tiled2Dmap.CLI.Tiled
{
    public class TiledProject
    {
        [JsonPropertyName("automappingRulesFile")]
        public string AutoMappingRulesFile { get; set; } = "";
        public List<string> Commands { get; set; } = new();
        [JsonPropertyName("extensionsPath")]
        public string ExtensionsPath { get; set; } = "extensions";
        public List<string> Folders { get; set; } = new();
        [JsonPropertyName("objectTypesFile")]
        public string ObjectTypesFile { get; set; } = "";

        [JsonIgnore]
        public string ProjectName { get { return new DirectoryInfo(ProjectDirectory).Name; } }
        [JsonIgnore]
        public string ProjectDirectory { get; init; }
        [JsonIgnore]
        public string ProjectFilePath { get { return $"{ProjectDirectory}/{this.ProjectName}.tiled-project"; } }


        public TiledProject(string ProjectDirectory)
        {
            this.ProjectDirectory = ProjectDirectory;
            this.Folders.Add(this.ProjectDirectory);
        }

        public void AddDirectory(string DirectoryPath)
        {
            if (!Directory.Exists(DirectoryPath)) Directory.CreateDirectory(DirectoryPath);
            if (!this.Folders.Contains(DirectoryPath)) this.Folders.Add(DirectoryPath);
        }
        public void Copy(string FilePath, string DestinationSubDirectory)
        {
            string fileName = new FileInfo(FilePath).Name;
            File.Copy(FilePath, $"{DestinationSubDirectory}/{fileName}", true);
        }

        public static TiledProject FromDmap(string ProjectDirectory, Utility.ClientResources ClientResources, Dmap.DmapFile DmapFile)
        {
            TiledProject tiledProject = new(ProjectDirectory);

            //Create the Tiled directory. 
            if (!Directory.Exists(Path.Combine(tiledProject.ProjectDirectory, "tiled"))) Directory.CreateDirectory(Path.Combine(tiledProject.ProjectDirectory, "tiled"));

            #region Setup Json Options
            JsonSerializerOptions jsOptions = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = new Json.LowerCaseNamingPolicy(),
                WriteIndented = true
            };
            jsOptions.Converters.Add(new Json.TiledLayerConverter());
            jsOptions.Converters.Add(new Json.TiledObjectConverter());
            jsOptions.Converters.Add(new Json.TileConverter());
            jsOptions.Converters.Add(new Json.TiledPropertyConverter());
            #endregion Setup Json Options

            #region Access/Height/Surface
            //Create the tile map for access/height/surface type.
            //This does not have access/height/surface for Scenes.

            int AccessGlobalId = 1;
            int accessTileGid = AccessGlobalId;
            //Create access subdirectory.
            tiledProject.AddDirectory(Path.Combine(tiledProject.ProjectDirectory, "tiled", "access"));
            //Save the portal image to the project directory.
            string accessImagePath = $"{Path.Combine(ProjectDirectory, "tiled", "access")}/access.png";
            Resources.access_64x64.Save(accessImagePath);

            //Create the Access Tilset

            TileSetFile accessTileSet = new()
            {
                Name = "ts_access",
                TileWidth = 64,
                TileHeight = 32,
                Image = Path.GetRelativePath(Path.Combine(tiledProject.ProjectDirectory, "tiled","access"), accessImagePath),
                ImageWidth = Resources.access_64x64.Width,
                ImageHeight = Resources.access_64x64.Height
            };

            //Save the portal tile set.
            string accessTileSetPath = $"{Path.Combine(tiledProject.ProjectDirectory, "tiled", "access")}/{accessTileSet.Name}.json";
            File.WriteAllText(accessTileSetPath, JsonSerializer.Serialize(accessTileSet, jsOptions));

            TiledMapFile accessMapFile = new()
            {
                WidthTiles = (int)DmapFile.SizeTiles.Width,
                HeightTiles = (int)DmapFile.SizeTiles.Height,
                TileWidth = Constants.DmapTileWidth,
                TileHeight = Constants.DmapTileHeight
            };

            //Add the portal tile set to the map file
            accessMapFile.TileSets.Add(new()
            {
                Source = Path.GetRelativePath(Path.Combine(tiledProject.ProjectDirectory, "tiled"), accessTileSetPath),
                FirstGId = AccessGlobalId
            });

            AccessGlobalId += 2; // Add two becase two tiles in the access tile set. 

            TileLayer accessLayer = new()
            {
                WidthTiles = accessMapFile.WidthTiles,
                HeightTiles = accessMapFile.HeightTiles,
                Name = "Access"
            };
            TileLayer heightLayer = new()
            {
                WidthTiles = accessMapFile.WidthTiles,
                HeightTiles = accessMapFile.HeightTiles,
                Name = "Height"
            };
            TileLayer surfaceTypeLayer = new()
            {
                WidthTiles = accessMapFile.WidthTiles,
                HeightTiles = accessMapFile.HeightTiles,
                Name = "Surface"
            };
            accessLayer.Data = new int[accessLayer.WidthTiles * accessLayer.HeightTiles];
            heightLayer.Data = new int[heightLayer.WidthTiles * heightLayer.HeightTiles];
            surfaceTypeLayer.Data = new int[surfaceTypeLayer.WidthTiles * surfaceTypeLayer.HeightTiles];

            Dictionary<int, int> observedSurfaces = new();
            Dictionary<int, int> observedHeights = new();

            for (int xidx = 0; xidx < DmapFile.SizeTiles.Width; xidx++)
            {
                for (int yidx = 0; yidx < DmapFile.SizeTiles.Height; yidx++)
                {
                    accessLayer.Data[xidx + yidx * accessLayer.WidthTiles] = accessTileGid + DmapFile.TileSet[xidx, yidx].Access;

                    //this initial loop add the observed surface and heights so they can be sorted for simplicity. 
                    if (!observedHeights.ContainsKey(DmapFile.TileSet[xidx, yidx].Height)) observedHeights.Add(DmapFile.TileSet[xidx, yidx].Height, 0);
                    if (!observedSurfaces.ContainsKey(DmapFile.TileSet[xidx, yidx].Surface)) observedSurfaces.Add(DmapFile.TileSet[xidx, yidx].Surface, 0);
                    //heightLayer.Data[xidx + yidx * heightLayer.WidthTiles] = DmapFile.TileSet[xidx, yidx].Height;
                    //surfaceTypeLayer.Data[xidx + yidx * surfaceTypeLayer.WidthTiles] = DmapFile.TileSet[xidx, yidx].Surface;
                }
            }

            int surfaceTileGId = AccessGlobalId;
            AccessGlobalId += observedSurfaces.Count;
            int heightTileGId = AccessGlobalId;


            //Generate Surface Tile set based on observed surface types.
            TileSetFile surfaceTileSet = TileSetFile.TileSetFileTextImage("surface", Path.Combine(tiledProject.ProjectDirectory, "tiled", "access"), observedSurfaces);

            //Generate observedHeights Tile set based on observed surface types.
            TileSetFile heightTileSet = TileSetFile.TileSetFileTextImage("height", Path.Combine(tiledProject.ProjectDirectory, "tiled", "access"), observedHeights);

            //Save the tile sets and add them to the map.
            string surfaceTileSetPath = $"{Path.Combine(tiledProject.ProjectDirectory, "tiled", "access")}/{surfaceTileSet.Name}.json";
            File.WriteAllText(surfaceTileSetPath, JsonSerializer.Serialize(surfaceTileSet, jsOptions));

            accessMapFile.TileSets.Add(new()
            {
                Source = Path.GetRelativePath(Path.Combine(tiledProject.ProjectDirectory, "tiled"), surfaceTileSetPath),
                FirstGId = surfaceTileGId
            });

            string heightTileSetPath = $"{Path.Combine(tiledProject.ProjectDirectory, "tiled", "access")}/{heightTileSet.Name}.json";
            File.WriteAllText(heightTileSetPath, JsonSerializer.Serialize(heightTileSet, jsOptions));

            accessMapFile.TileSets.Add(new()
            {
                Source = Path.GetRelativePath(Path.Combine(tiledProject.ProjectDirectory, "tiled"), heightTileSetPath),
                FirstGId = heightTileGId
            });

            for (int xidx = 0; xidx < DmapFile.SizeTiles.Width; xidx++)
            {
                for (int yidx = 0; yidx < DmapFile.SizeTiles.Height; yidx++)
                {
                    heightLayer.Data[xidx + yidx * heightLayer.WidthTiles] = observedHeights[DmapFile.TileSet[xidx, yidx].Height] + heightTileGId;
                    surfaceTypeLayer.Data[xidx + yidx * surfaceTypeLayer.WidthTiles] = observedSurfaces[DmapFile.TileSet[xidx, yidx].Surface] + surfaceTileGId;
                }
            }


            accessMapFile.Layers.Add(accessLayer);
            accessMapFile.Layers.Add(heightLayer);
            accessMapFile.Layers.Add(surfaceTypeLayer);

            File.WriteAllText($"{Path.Combine(tiledProject.ProjectDirectory, "tiled")}/map_access.json", JsonSerializer.Serialize(accessMapFile, jsOptions));
            #endregion Access/Height/Surface

            int NextGlobalTileId = 1;

            #region Background
            //Create and slice up the background images.
            tiledProject.AddDirectory(Path.Combine(tiledProject.ProjectDirectory, "tiled", "background"));

            //Stitch up the puzzle file.
            PuzzleFile backgroundPuzzle = new PuzzleFile(ClientResources.ClientDirectory, DmapFile.PuzzleFile);

            IsometricSliceResult bgSliceResults = new();

            using (ImageServices.Stitch imageStitch = new(ClientResources, backgroundPuzzle))
            {
                CordConverter cordConverter = new(new Size((int)DmapFile.SizeTiles.Width, (int)DmapFile.SizeTiles.Height), imageStitch.Image.Size);
                IsometricSlice isometricSlicer = new(ConsoleAppLogger.CreateLogger<IsometricSlice>(), "background", ProjectDirectory, imageStitch.Image, cordConverter);
                bgSliceResults = isometricSlicer.Slice();
            }

            //(backgroundTileSet, backgroundTileLayer) = TileSetFile.TileSetFromPuzzleFile("background", ClientResources, tiledProject._projectDirectory, DmapFile.PuzzleFile, DmapFile.SizeTiles);

            string backgroundtileSetPath = $"{Path.Combine(tiledProject.ProjectDirectory, "tiled")}/{bgSliceResults.TileSetFile.Name}.json";
            File.WriteAllText(backgroundtileSetPath, JsonSerializer.Serialize(bgSliceResults.TileSetFile, jsOptions));

            TiledMapFile mainMapFile = new()
            {
                WidthTiles = bgSliceResults.TileLayer.WidthTiles,
                HeightTiles = bgSliceResults.TileLayer.HeightTiles,
                TileWidth = bgSliceResults.TileWidth,
                TileHeight = bgSliceResults.TileHeight
            };

            mainMapFile.Layers.Add(bgSliceResults.TileLayer);
            mainMapFile.TileSets.Add(new()
            {
                FirstGId = NextGlobalTileId,
                Source = Path.GetRelativePath(Path.Combine(tiledProject.ProjectDirectory, "tiled"), backgroundtileSetPath)
            });
            NextGlobalTileId += bgSliceResults.TileSetFile.TileCount;
            #endregion Background

            #region Portals
            //Create portal subdirectory.
            tiledProject.AddDirectory(Path.Combine(tiledProject.ProjectDirectory, "tiled", "portal"));
            //Save the portal image to the project directory.
            string portalImagePath = $"{Path.Combine(ProjectDirectory, "tiled", "portal")}/portal1.png";
            Resources.exit_128x64.Save(portalImagePath);

            //Create the portal tile set
            TileSetFile portalTileSet = new()
            {
                Name = "ts_portal",
                TileWidth = Resources.exit_128x64.Width,
                TileHeight = Resources.exit_128x64.Height
            };

            portalTileSet.Tiles.Add(new Tile()
            {
                Image = Path.GetRelativePath(Path.Combine(tiledProject.ProjectDirectory, "tiled"), portalImagePath),
                ImageWidth = portalTileSet.TileWidth,
                ImageHeight = portalTileSet.TileHeight,
                Id = 0
            });

            //Save the portal tile set.
            string portalTileSetPath = $"{Path.Combine(tiledProject.ProjectDirectory, "tiled")}/ts_portal.json";
            File.WriteAllText(portalTileSetPath, JsonSerializer.Serialize(portalTileSet, jsOptions));

            //Add the portal tile set to the map file
            mainMapFile.TileSets.Add(new()
            {
                Source = Path.GetRelativePath(Path.Combine(tiledProject.ProjectDirectory, "tiled"), portalTileSetPath),
                FirstGId = NextGlobalTileId
            });

            ObjectLayer portalLayer = new()
            {
                Name = "Portals",
            };
            int portalIdx = 0;
            foreach (var portal in DmapFile.Portals)
            {
                portalLayer.Objects.Add(new PortalObject(portal.Id)
                {
                    Name = $"Portal_{portalIdx++}",
                    XPixels = (int)(portal.Position.X * Constants.DmapTileHeight),// + Resources.exit_128x64.Width / 2,
                    YPixels = (int)(portal.Position.Y * Constants.DmapTileHeight),// + Resources.exit_128x64.Height / 2, //Intentionally tile height.
                    Width = Resources.exit_128x64.Width,
                    Height = Resources.exit_128x64.Height,
                    GId = NextGlobalTileId
                });
            }
            //Add portal layer to map.
            mainMapFile.Layers.Add(portalLayer);

            //Increase global id for next tile set start id. 
            NextGlobalTileId += portalTileSet.TileCount;
            #endregion Portals

            #region Effects
            ObjectLayer effectLayer = new ObjectLayer()
            {
                Name = "Effects"
            };
            int effectIdx = 0;
            foreach (var effect in DmapFile.Effects)
            {
                //TODO: Determine is the bgPos.Y needs to be offset by 16 (tileheight / 2) for correct effect placement.
                Utility.PixelPosition bgPos = new()
                {
                    X = effect.Position.X - (int)(DmapFile.SizeTiles.Width * Constants.DmapTileWidth / 2),
                    Y = effect.Position.Y + Constants.DmapTileHeight / 2
                };
                effectLayer.Objects.Add(new EffectObject(effect.EffectName)
                {
                    Name = $"Effect_{effectIdx}",
                    XPixels = (/*orthX compensation*/ (bgPos.X * 0.5)) + (/*orthY compensation*/ (int)bgPos.Y - 16),
                    YPixels = -(/*orthX compensation*/ (bgPos.X * 0.5)) + (/*orthY compensation*/ (int)bgPos.Y - 16),
                    Width = Constants.DmapTileWidth,
                    Height = Constants.DmapTileWidth
                });
                //TODO :Copy effects to tiled directory. Determine what files are required to copy...
            }

            //Add effect layer to map.
            mainMapFile.Layers.Add(effectLayer);
            #endregion Effects

            #region Sounds
            tiledProject.AddDirectory(Path.Combine(tiledProject.ProjectDirectory, "tiled", "sound"));
            ObjectLayer soundLayer = new ObjectLayer()
            {
                Name = "Sounds"
            };
            int soundIdx = 0;
            foreach (var sound in DmapFile.Sounds)
            {
                //TODO: Determine is Range is in tiles or pixels. and then use ellipse.
                //TODO: Determine is the bgPos.Y needs to be offset by 16 (tileheight / 2) for correct effect placement.
                Utility.PixelPosition bgPos = new()
                {
                    X = sound.Position.X - (int)(DmapFile.SizeTiles.Width * Constants.DmapTileWidth / 2),
                    Y = sound.Position.Y + Constants.DmapTileHeight / 2
                };
                soundLayer.Objects.Add(new SoundObject(sound.SoundFile, (int)sound.Volume, (int)sound.Range)
                {
                    Name = $"Sound_{soundIdx++}",
                    XPixels = (/*orthX compensation*/ (bgPos.X * 0.5)) + (/*orthY compensation*/ (int)bgPos.Y - 16),
                    YPixels = -(/*orthX compensation*/ (bgPos.X * 0.5)) + (/*orthY compensation*/ (int)bgPos.Y - 16),
                    Width = Constants.DmapTileWidth,
                    Height = Constants.DmapTileWidth
                });
                //Copy the file to tile project sound dir.
                ClientResources.Copy(Path.Combine(tiledProject.ProjectDirectory, "tiled", "sound"), sound.SoundFile);
            }

            //Add sound layer to map
            mainMapFile.Layers.Add(soundLayer);
            #endregion Sounds

            #region Cover
            string coverProjDir = Path.Combine(tiledProject.ProjectDirectory, "tiled", "cover");
            tiledProject.AddDirectory(coverProjDir);
            ObjectLayer coverLayer = new ObjectLayer()
            {
                Name = "Covers"
            };
            TileSetFile coverTileSet = new TileSetFile()
            {
                Name = "ts_cover",
                ObjectAlignment = "topleft"
            };
            int maxTileWidth = 0;
            int maxTileHeight = 0;
            int coverIdx = 0;
            //Create a store so we don't duplicate assets.
            Dictionary<string, CoverObject> coverCache = new();// Key- anipath-aniname
            Dictionary<string, Dmap.AniFile> aniCache = new(); // Key- ani path

            foreach (var cover in DmapFile.Covers)
            {
                string cacheKey = $"{cover.AniPath}-{cover.AniName}";

                //Load the ani file if we haven't already.
                Dmap.AniFile aniFile = null;
                if (!aniCache.TryGetValue(cover.AniPath, out aniFile))
                {
                    aniFile = new Dmap.AniFile(ClientResources.ClientDirectory, cover.AniPath);
                    aniCache.Add(cover.AniPath, aniFile);
                }

                //If it hasn't been created we need to create the tile in the tileset.
                CoverObject coverObj = null;
                if (!coverCache.TryGetValue(cacheKey, out coverObj))
                {
                    //This doesn't exist in the current tile set.
                    Dmap.Ani coverAni = aniFile.Anis[cover.AniName];
                    coverObj = new CoverObject(cover.BaseSize.Width, cover.BaseSize.Height);
                    if (coverAni.Frames.Count > 1)
                    {
                        //Animated frame.
                        AnimatedTile animatedTile = new();
                        foreach (var aniFrame in coverAni.Frames)
                        {
                            //Convert the DDS to a png.
                            string coverBmpPath = Path.Combine(coverProjDir, aniFrame);
                            Directory.CreateDirectory(Path.GetDirectoryName(coverBmpPath));
                            using (Bitmap coverBmp = ImageServices.DDSConvert.StreamToPng(ClientResources.GetFile(aniFrame)))
                            {
                                //TOOD: Maintain original relative path
                                coverBmp.Save(coverBmpPath);

                                //Add this tile to the tileset.
                                coverTileSet.Tiles.Add(new Tile()
                                {
                                    Id = coverTileSet.TileCount,
                                    ImageWidth = coverBmp.Width,
                                    ImageHeight = coverBmp.Height,
                                    Image = Path.GetRelativePath(coverProjDir, coverBmpPath)
                                });

                                if (coverBmp.Width > coverObj.Width) coverObj.Width = coverBmp.Width;
                                if (coverBmp.Height > coverObj.Height) coverObj.Height = coverBmp.Height;
                            }

                            //Add the frame to the animatedTile.
                            animatedTile.Frames.Add(new()
                            {
                                Duration = (int)cover.AnimationInterval,
                                TileId = coverTileSet.TileCount - 1 //-1
                            });
                        }

                        //Add the animated frame to the tile set.
                        animatedTile.Id = coverTileSet.TileCount;
                        coverTileSet.Tiles.Add(animatedTile);

                        coverObj.GId = NextGlobalTileId + animatedTile.Id;

                    }
                    else
                    {
                        string coverAniRelPath = coverAni.Frames.Peek();
                        string coverBmpPath = Path.Combine(coverProjDir, coverAniRelPath.Replace(".dds", ".png"));
                        Directory.CreateDirectory(Path.GetDirectoryName(coverBmpPath));

                        //The tile is not animated.
                        using (Bitmap coverBmp = ImageServices.DDSConvert.StreamToPng(ClientResources.GetFile(coverAniRelPath)))
                        {
                            //TOOD: Maintain original relative path
                            coverBmp.Save(coverBmpPath);

                            //Add this tile to the tileset.
                            coverTileSet.Tiles.Add(new Tile()
                            {
                                Id = coverTileSet.TileCount,
                                ImageWidth = coverBmp.Width,
                                ImageHeight = coverBmp.Height,
                                Image = Path.GetRelativePath(coverProjDir, coverBmpPath)
                            });
                            coverObj.Width = coverBmp.Width;
                            coverObj.Height = coverBmp.Height;
                        }
                        coverObj.GId = NextGlobalTileId + coverTileSet.TileCount - 1; // Remove one because it is the most recently added tile.
                    }
                    coverCache.Add(cacheKey, coverObj);
                }
                CoverObject newCoverObject = new()
                {
                    Name = $"Cover_{coverIdx++}",
                    XPixels = (cover.Position.X * Constants.DmapTileHeight) - (/*orthX compensation*/ (cover.Offset.X * 0.5)) - (/*orthY compensation*/ (int)cover.Offset.Y - 16),
                    YPixels = (cover.Position.Y * Constants.DmapTileHeight) + (/*orthX compensation*/ (cover.Offset.X * 0.5)) - (/*orthY compensation*/ (int)cover.Offset.Y - 16),
                    Width = coverObj.Width,
                    Height = coverObj.Height,
                    GId = coverObj.GId
                };
                if (coverObj.Width > maxTileWidth) maxTileWidth = coverObj.Width;
                if (coverObj.Height > maxTileHeight) maxTileHeight = coverObj.Height;

                coverLayer.Objects.Add(newCoverObject);

            }
            coverTileSet.TileWidth = maxTileWidth;
            coverTileSet.TileHeight = maxTileHeight;

            //Save the tileset
            string coverTileSetPath = $"{Path.Combine(tiledProject.ProjectDirectory, "tiled")}/{coverTileSet.Name}.json";
            File.WriteAllText(coverTileSetPath, JsonSerializer.Serialize(coverTileSet, jsOptions));

            mainMapFile.TileSets.Add(new InternalTileSet()
            {
                FirstGId = NextGlobalTileId,
                Source = Path.GetRelativePath(Path.Combine(tiledProject.ProjectDirectory, "tiled"), coverTileSetPath)
            });
            //add the object layer to main map
            mainMapFile.Layers.Add(coverLayer);

            NextGlobalTileId += coverTileSet.TileCount;
            #endregion

            #region Scenes
            if (DmapFile.TerrainScenes.Count > 0)
            {
                Log.Error("Scenes not yet supoprted");

                tiledProject.AddDirectory(Path.Combine(tiledProject.ProjectDirectory, "tiled", "scenes"));

                foreach (var scene in DmapFile.TerrainScenes)
                {
                    //Each terrain scene will have its own directory, map, and access map.
                    //Each scene will have an object on the main map with a reference to the scene directory.
                }
            }
            #endregion

            #region SceneLayers
            if (DmapFile.SceneLayers.Count > 0)
            {
                Log.Error("Scene Layers not yet supoprted");
            }
            #endregion SceneLayers

            #region Puzzle
            if (DmapFile.Puzzles.Count > 0)
            {
               Log.Error("Additional Puzzles not yet supoprted");
            }
            #endregion Puzzle   

            //Save the tiledProject and main map
            string mainMapPath = $"{Path.Combine(tiledProject.ProjectDirectory, "tiled")}/map_main.json";
            File.WriteAllText(mainMapPath, JsonSerializer.Serialize(mainMapFile, jsOptions));

            File.WriteAllText(tiledProject.ProjectFilePath, JsonSerializer.Serialize(tiledProject, jsOptions));

            return tiledProject;
        }
    }
}
