using System;
using System.IO;
using SevenZip;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tiled2Dmap.CLI.Extensions;
using Tiled2Dmap.CLI.Utility;

namespace Tiled2Dmap.CLI.Dmap
{
    public class DmapFile
    {
        public string DmapName { get; set; }
        public string DmapPath { get; set; }

        public byte[] Header { get; set; }
        public uint MapVersion { get; set; }
        public bool IsNew { get { return DmapPath.ToLower().Contains("_new"); } }
        public string? PuzzleFile { get; set; }
        public string? PuxFile { get; set; }
        /// <summary>
        /// Size of the map in accessible tiles.
        /// </summary>
        public Size SizeTiles { get; set; }
        public Tile[,] TileSet { get; set; }
        public List<Portal> Portals { get; set; } = new();
        public List<TerrainScene> TerrainScenes { get; set; } = new();
        public List<Cover> Covers { get; set; } = new();
        public List<string> Puzzles { get; set; } = new();
        public List<Effect> Effects { get; set; } = new();
        public List<Sound> Sounds { get; set; } = new();
        public List<SceneLayer> SceneLayers { get; set; } = new();
        public List<EffectNew> EffectNews { get; set; } = new();
        public List<Unknown1> Unknown1Objs { get; set; } = new();
        public List<Unknown2> Unknown2Objs { get; set; } = new();

        public DmapFile() { }
        /// <summary>
        /// Loads a conquer Dmap file
        /// </summary>
        /// <param name="ClientPath">Root Directory of Conquer client</param>
        /// <param name="DmapPath">Relative or absolute path to Dmap file</param>
        public DmapFile(string DmapPath, string? ClientPath = null)
        {
            this.DmapPath = DmapPath;
            this.DmapName = Path.GetFileNameWithoutExtension(DmapPath);

            if (!Path.IsPathFullyQualified(DmapPath))
            {
                this.DmapPath = $"{ClientPath ?? ""}/{DmapPath}";
            }

            if (!File.Exists(this.DmapPath))
                throw new FileNotFoundException($"The specific dmap could not be found at {this.DmapPath}");

            LoadFile();
        }
        private void LoadFile()
        {
            if (!File.Exists(this.DmapPath))
                throw new FileNotFoundException($"The specific dmap could not be found at {this.DmapPath}");

            if (Path.GetExtension(this.DmapPath) == ".7z" || Path.GetExtension(this.DmapPath) == ".zmap")
            {
                //Need to decompress the dmap from a 7-zip archive.
                using (MemoryStream memoryStream = new())
                {
                    new SevenZipExtractor(this.DmapPath).ExtractFile(0, (Stream)memoryStream);
                    memoryStream.Position = 0L;
                    Load((Stream)memoryStream);
                }
            }
            else
            {
                //it should be a dmap.
                Load((Stream)new FileStream(this.DmapPath, FileMode.Open));
            }
        }

        private void Load(Stream stream)
        {
            using (BinaryReader br = new(stream))
            {
                //This is sometimes a string, sometimes a Long.
                this.Header = br.ReadBytes(8);
                this.MapVersion = BitConverter.ToUInt32(this.Header, 0);
                string headerStr = ASCIIEncoding.ASCII.GetString(this.Header);
                if (headerStr.StartsWith("DMAP"))
                    this.MapVersion = 101;

                string puzzleFile = br.ReadASCIIString(260);
                
                if (puzzleFile.EndsWith("pux")) PuxFile = puzzleFile;
                else PuzzleFile = puzzleFile;

                //if ((PuzzleFile.ToLower()).EndsWith("pux")) throw new Exception("PUX file not supported");

                this.SizeTiles = br.ReadSize();
                this.TileSet = new Tile[this.SizeTiles.Width, this.SizeTiles.Height];

                uint val = BitConverter.ToUInt32(this.Header, 4);
                Console.WriteLine($"Path: {DmapPath}, Version: {MapVersion}, Header: {headerStr}, Val {val}");

                if (IsNew && MapVersion < 1005) Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!NEW < 1005");
                //Load tile information
                for (int tileY = 0; tileY < this.SizeTiles.Height; tileY++)
                {
                    for (int tileX = 0; tileX < this.SizeTiles.Width; tileX++)
                    {
                        this.TileSet[tileX, tileY] = new Tile
                        {
                            NoAccess = br.ReadUInt16(),
                            Surface = br.ReadUInt16(),
                            Height = br.ReadInt16()

                        };
                    }
                    //Integrity check. 
                    br.ReadInt32();
                }

                //Load game portals.
                int numPortals = br.ReadInt32();
                for (int portalIdx = 0; portalIdx < numPortals; portalIdx++)
                {
                    this.Portals.Add(new Portal
                    {
                        Position = br.ReadTilePosition(),
                        Id = br.ReadUInt32()
                    });
                }

                if (this.MapVersion >= 0x3ee)
                {
                    uint itemCount = br.ReadUInt32();
                    for (int itemIdx = 0; itemIdx < itemCount; itemIdx++)
                    {
                        uint itemType = br.ReadUInt32();
                        switch (itemType)
                        {
                            case 0x18:
                                var coverItem = new Cover()
                                {
                                    AniPath = br.ReadASCIIString(260),
                                    AniName = br.ReadASCIIString(128),
                                    Position = br.ReadTilePosition(),
                                    BaseSize = br.ReadSize(),
                                    Offset = br.ReadPixelPosition(),
                                    AnimationInterval = br.ReadUInt32()
                                };
                                Covers.Add(coverItem);
                                uint unk = br.ReadUInt32();
                                //Log.Info($"CoverItem: {coverItem.AniPath}-{coverItem.AniName}");
                                break;
                            default:
                                Log.Warn($"Unknown Map item type: {itemType}");
                                break;
                        }
                    }
                }

                //Load the interactive layer.
                int numObjects = br.ReadInt32();
                for (int objIdx = 0; objIdx < numObjects; objIdx++)
                {
                    uint objType = br.ReadUInt32();
                    switch ((MapObjectType)objType)
                    {
                        case MapObjectType.Terrain:
                            this.TerrainScenes.Add(new TerrainScene()
                            {
                                SceneFile = br.ReadASCIIString(260),
                                Position = br.ReadTilePosition()
                            });
                            break;
                        case MapObjectType.Cover:
                            Cover newCover = new()
                            {
                                AniPath = br.ReadASCIIString(260),
                                AniName = br.ReadASCIIString(128),
                                Position = br.ReadTilePosition(),
                                BaseSize = br.ReadSize(),
                                Offset = br.ReadPixelPosition(),
                                AnimationInterval = br.ReadUInt32()
                            };
                            this.Covers.Add(newCover);
                            if (this.MapVersion > 0x3ec || DmapPath.EndsWith("zmap")) // Offset 0x94 form CGameMap
                            {
                                uint unk = br.ReadUInt32();
                                if (unk != 0)
                                    Console.WriteLine($"!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! not 0 {unk:X4}");
                            }
                            break;
                        case MapObjectType.Puzzle:
                            this.Puzzles.Add(br.ReadASCIIString(260));
                            break;
                        case MapObjectType.Effect:
                            this.Effects.Add(new Effect()
                            {
                                EffectName = br.ReadASCIIString(64),
                                Position = br.ReadPixelPosition()
                            });
                            break;
                        case MapObjectType.Sound:
                            this.Sounds.Add(new Sound()
                            {
                                SoundFile = br.ReadASCIIString(260),
                                Position = br.ReadPixelPosition(),
                                Volume = br.ReadUInt32(),
                                Range = br.ReadUInt32()
                            });
                            if (DmapPath.EndsWith("zmap"))
                                _ = br.ReadUInt32(); //Interval.
                            break;
                        case MapObjectType.EffectNew:
                            EffectNews.Add(new()
                            {
                                EffectName = br.ReadASCIIString(60),
                                Position = br.ReadPixelPosition(),
                                Unk1_u32 = br.ReadUInt32(),
                                Unk2_u32 = br.ReadUInt32(),
                                Unk3_u32 = br.ReadUInt32(),
                                Unk4_u32 = br.ReadUInt32(),
                                Unk5_u32 = br.ReadUInt32(),
                                Unk6_u32 = br.ReadUInt32(),
                                Unk7_u32 = br.ReadUInt32()

                            });
                            break;
                        case MapObjectType.Unknown1:
                            Unknown1Objs.Add(new()
                            {
                                AniFile = br.ReadASCIIString(260),
                                AniName = br.ReadASCIIString(128),
                                Unk0_i16 = br.ReadInt16(),
                                Unk1_i16 = br.ReadInt16(),
                                Unk2_i32 = br.ReadInt32(),
                                Unk3_i32 = br.ReadInt32(),
                                Unk4_i32 = br.ReadInt32(),
                                Unk5_i32 = br.ReadInt32(),
                                Unk6_i32 = br.ReadInt32(),
                                Unk7_i32 = br.ReadInt32(),
                                Unk8_i32 = br.ReadInt32(),
                                Unk9_i32 = br.ReadInt32()
                            });
                            break;
                        case MapObjectType.Unknown2:
                            Unknown2Objs.Add(new()
                            {
                                Name = br.ReadASCIIString(260),
                                Unk0_u32 = br.ReadUInt32(),
                                Position = br.ReadTilePosition()

                            });
                            break;
                        default:
                            Log.Warn($"Unknown map object type: 0x{objType:X2}");
                            break;
                    }
                }

                //Load Additional Layers
                int numLayers = br.ReadInt32();
                for (int layerIdx = 0; layerIdx < numLayers; layerIdx++)
                {
                    uint layIdx = br.ReadUInt32();
                    uint layType = br.ReadUInt32();
                    switch (layType)
                    {
                        case 4:
                            SceneLayer sceneLayer = new()
                            {
                                Index = layIdx,
                                MoveRate = br.ReadPixelPosition()
                            };
                            if (this.MapVersion > 0x3ec)
                            {
                                uint unk1 = br.ReadUInt32();
                                uint unk2 = br.ReadUInt32();
                                uint unk3 = br.ReadUInt32();
                            }
                            uint objAmt = br.ReadUInt32();
                            for (int objIdx = 0; objIdx < objAmt; objIdx++)
                            {
                                uint objType = br.ReadUInt32();

                                switch ((MapObjectType)objType)
                                {
                                    case MapObjectType.Terrain:
                                        sceneLayer.TerrainScenes.Add(new TerrainScene()
                                        {
                                            SceneFile = br.ReadASCIIString(260),
                                            Position = br.ReadTilePosition()
                                        });
                                        break;
                                    case MapObjectType.MapScene:
                                        string aniPath = br.ReadASCIIString(0x104);
                                        string aniName = br.ReadASCIIString(0x80);
                                        uint unk1 = br.ReadUInt32();
                                        uint unk2 = br.ReadUInt32();
                                        uint unk3 = br.ReadUInt32();
                                        uint unk4 = br.ReadUInt32();
                                        uint unk5 = br.ReadUInt32();
                                        uint unk6 = br.ReadUInt32();
                                        break;
                                    case MapObjectType.Puzzle:
                                        sceneLayer.Puzzles.Add(br.ReadASCIIString(260));
                                        break;
                                    case MapObjectType.Effect:
                                        sceneLayer.Effects.Add(new Effect()
                                        {
                                            EffectName = br.ReadASCIIString(64),
                                            Position = br.ReadPixelPosition()
                                        });
                                        break;
                                    case MapObjectType.EffectNew:
                                        sceneLayer.EffectNews.Add(new()
                                        {
                                            EffectName = br.ReadASCIIString(60),
                                            Position = br.ReadPixelPosition(),
                                            Unk1_u32 = br.ReadUInt32(),
                                            Unk2_u32 = br.ReadUInt32(),
                                            Unk3_u32 = br.ReadUInt32(),
                                            Unk4_u32 = br.ReadUInt32(),
                                            Unk5_u32 = br.ReadUInt32(),
                                            Unk6_u32 = br.ReadUInt32(),
                                            Unk7_u32 = br.ReadUInt32()

                                        });
                                        break;
                                    default: Log.Warn($"Unsupport Additional Layer Map Object {objType}"); break;
                                }
                            }
                            this.SceneLayers.Add(sceneLayer);
                            break;
                        default: Log.Warn($"Unknown Additional Layer Type: {layType}"); break;
                    }
                }

                if (this.MapVersion > 0x3ec)
                {
                    _ = br.ReadBytes(8); // Unk
                }

                Console.Write($"Finished reading {this.DmapPath}, {numLayers} additional layers. ");
                var consoleColor = Console.BackgroundColor;
                long last = -1;
                bool notDone = false;
                if (br.BaseStream.Position != br.BaseStream.Length)
                    Console.BackgroundColor = ConsoleColor.Red;
                Console.Write($"{br.BaseStream.Position}/{br.BaseStream.Length}");
                Console.BackgroundColor = consoleColor;
                Console.WriteLine();
            }
        }

        public void Save(string OutputDirectory)
        {
            string outputPath = Path.Combine(OutputDirectory, DmapPath);
            string outputDir = Path.GetDirectoryName(outputPath);
            Directory.CreateDirectory(outputDir);
            Save(File.OpenWrite(outputPath));
        }
        public void Save(Stream stream)
        {
            BinaryWriter bw = new(stream);

            bw.Write(this.Header);
            bw.WriteASCIIString(this.PuzzleFile, 260);
            bw.Write(this.SizeTiles);

            //Write tile data
            for (int tileY = 0; tileY < this.SizeTiles.Height; tileY++)
            {
                uint integrityCheck = 0;
                for (int tileX = 0; tileX < this.SizeTiles.Width; tileX++)
                {
                    var tile = this.TileSet[tileX, tileY];
                    bw.Write(tile.NoAccess);
                    bw.Write(tile.Surface);
                    bw.Write(tile.Height);
                    integrityCheck += (uint)(tile.Surface + tileY + 1) * tile.NoAccess +
                        (uint)((tileX + tile.Surface + 1) * (tile.Height + 2U));

                }
                //Integrity check. 
                bw.Write(integrityCheck);
            }

            //Write Portal
            bw.Write(this.Portals.Count);
            foreach (var portal in this.Portals)
            {
                bw.Write(portal.Position);
                bw.Write(portal.Id);
            }

            //Write Interactive Layers
            int objCount = this.TerrainScenes.Count + this.Covers.Count + this.Puzzles.Count
                + this.Effects.Count + this.Sounds.Count;
            bw.Write(objCount);
            foreach (var terrainScene in this.TerrainScenes)
            {
                bw.Write((uint)MapObjectType.Terrain);
                bw.WriteASCIIString(terrainScene.SceneFile, 260);
                bw.Write(terrainScene.Position);
            }
            foreach (var cover in this.Covers)
            {
                bw.Write((uint)MapObjectType.Cover);
                bw.WriteASCIIString(cover.AniPath, 260);
                bw.WriteASCIIString(cover.AniName, 128);
                bw.Write(cover.Position);
                bw.Write(cover.BaseSize);
                bw.Write(cover.Offset);
                bw.Write(cover.AnimationInterval);
            }
            foreach (var puzzle in this.Puzzles)
            {
                bw.Write((uint)MapObjectType.Puzzle);
                bw.WriteASCIIString(puzzle, 260);
            }
            foreach (var effect in this.Effects)
            {
                bw.Write((uint)MapObjectType.Effect);
                bw.WriteASCIIString(effect.EffectName, 64);
                bw.Write(effect.Position);
            }
            foreach (var sound in this.Sounds)
            {
                bw.Write((uint)MapObjectType.Sound);
                bw.WriteASCIIString(sound.SoundFile, 260);
                bw.Write(sound.Position);
                bw.Write(sound.Volume);
                bw.Write(sound.Range);
            }

            //Save Additional Layers.
            bw.Write(this.SceneLayers.Count);
            foreach (var sceneLayer in this.SceneLayers)
            {
                bw.Write(sceneLayer.Index);
                bw.Write(0x04); //SCENE LAYER TYPE

                bw.Write(sceneLayer.MoveRate);

                bw.Write(sceneLayer.Puzzles.Count + sceneLayer.TerrainScenes.Count);
                foreach (var puzzle in sceneLayer.Puzzles)
                {
                    bw.Write((uint)MapObjectType.Puzzle);
                    bw.WriteASCIIString(puzzle, 260);
                }
                foreach (var terrainScene in sceneLayer.TerrainScenes)
                {
                    bw.Write((uint)MapObjectType.Terrain);
                    bw.WriteASCIIString(terrainScene.SceneFile, 260);
                    bw.Write(terrainScene.Position);
                }
            }

            Log.Info($"Finished Saving map {bw.BaseStream.Position}");
        }
    }
}
