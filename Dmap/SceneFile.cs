using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tiled2Dmap.CLI.Extensions;
using Tiled2Dmap.CLI.Utility;

namespace Tiled2Dmap.CLI.Dmap
{
    public class SceneFile
    {
        public uint ScenePartCount { get; set; }
        public List<ScenePart> SceneParts { get; set; } = new();
        public string SceneFilePath { get; set; }

        private string ClientPath;

        public SceneFile(string ClientPath, string SceneFilePath)
        {
            this.ClientPath = ClientPath;
            this.SceneFilePath = SceneFilePath;
            this.Load();
        }
        public string Save(string ProjectDirectory)
        {
            if (!Directory.Exists(Path.Combine(ProjectDirectory, "map/Scene")))
                Directory.CreateDirectory(Path.Combine(ProjectDirectory, "map/Scene"));

            using (BinaryWriter bw = new BinaryWriter(File.OpenWrite(Path.Combine(ProjectDirectory, this.SceneFilePath))))
            {
                bw.Write(this.SceneParts.Count);
                foreach (var scenePart in this.SceneParts)
                {
                    bw.WriteASCIIString(scenePart.AniPath, 256);
                    bw.WriteASCIIString(scenePart.AniName, 64);
                    bw.Write(scenePart.PixelLocation);
                    bw.Write(scenePart.Interval);
                    bw.Write(scenePart.Size);
                    bw.Write(scenePart.Thickness);
                    bw.Write(scenePart.TileOffset);
                    bw.Write(scenePart.OffsetElevation);
                    for (int yidx = 0; yidx < scenePart.Size.Height; yidx++)
                    {
                        for (int xidx = 0; xidx < scenePart.Size.Width; xidx++)
                        {
                            bw.Write(scenePart.Tiles[xidx, yidx].NoAccess);
                            bw.Write(scenePart.Tiles[xidx, yidx].Surface);
                            bw.Write(scenePart.Tiles[xidx, yidx].Height);
                        }
                    }
                }
            }
            //Need to return relative path
            return this.SceneFilePath;
        }

        private void Load()
        {
            if (Path.IsPathFullyQualified(this.SceneFilePath))
                this.SceneFilePath = Path.GetRelativePath(this.ClientPath, this.SceneFilePath);
            string sceneFilePath = Path.Combine(this.ClientPath, this.SceneFilePath);

            if (!File.Exists(sceneFilePath)) throw new FileNotFoundException($"Scene File not found at {sceneFilePath}");

            using (BinaryReader br = new BinaryReader(File.OpenRead(sceneFilePath)))
            {
                this.ScenePartCount = br.ReadUInt32();
                for (int idx = 0; idx < this.ScenePartCount; idx++)
                {
                    var scene = new ScenePart()
                    {
                        AniPath = br.ReadASCIIString(256),
                        AniName = br.ReadASCIIString(64),
                        PixelLocation = br.ReadPixelOffset(),
                        Interval = br.ReadUInt32(),
                        Size = br.ReadSize(),
                        Thickness = br.ReadUInt32(),
                        TileOffset = br.ReadTileOffset(),
                        OffsetElevation = br.ReadInt32(),
                    };
                    scene.Tiles = new SceneTile[scene.Size.Width, scene.Size.Height];
                    for (int yidx = 0; yidx < scene.Size.Height; yidx++)
                    {
                        for (int xidx = 0; xidx < scene.Size.Width; xidx++)
                        {
                            scene.Tiles[xidx, yidx] = new SceneTile
                            {
                                NoAccess = br.ReadUInt32(),
                                Surface = br.ReadUInt32(),
                                Height = br.ReadInt32()
                            };
                        }
                    }
                    this.SceneParts.Add(scene);
                }
                /*
                Console.Write($"Finished reading {this.SceneFilePath} ");
                var consoleColor = Console.BackgroundColor;
                if (br.BaseStream.Position != br.BaseStream.Length)
                    Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine($"{br.BaseStream.Position}/{br.BaseStream.Length}");
                Console.BackgroundColor = consoleColor;
                */
            }
        }
    }
}
