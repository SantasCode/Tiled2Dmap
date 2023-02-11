using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Tiled2Dmap.CLI.Extensions;
using Tiled2Dmap.CLI.Utility;
using SixLabors.ImageSharp.PixelFormats;
using Tiled2Dmap.CLI.ImageServices;

namespace Tiled2Dmap.CLI.Dmap
{
    public class PuzzleFile
    {
        private static readonly byte[] PUZZLE = new byte[8] { 80, 85, 90, 90, 76, 69, 0, 0 };
        private static readonly byte[] PUZZLE2 = new byte[8] { 80, 85, 90, 90, 76, 69, 50, 0 };

        public string Header { get; set; }
        public string AniFile { get; set; }
        public Size Size { get; set; }
        public ushort[,] PuzzleTiles { get; set; }
        public PixelPosition RollSpeed { get; set; }
        public string PuzzlePath { get; set; }

        private string ClientPath;
        private int width = -1;

        public PuzzleFile(string PuzzlePath){ this.PuzzlePath = PuzzlePath; }
        public PuzzleFile(string ClientPath, string PuzzlePath)
        {
            this.PuzzlePath = PuzzlePath;
            this.ClientPath = ClientPath;
            this.Load();
        }

        public string Save(string ProjectDirectory)
        {
            if (!Directory.Exists(Path.Combine(ProjectDirectory, "map/puzzle")))
                Directory.CreateDirectory(Path.Combine(ProjectDirectory, "map/puzzle"));

            using (BinaryWriter writer = new BinaryWriter(File.OpenWrite(Path.Combine(ProjectDirectory, PuzzlePath))))
            {
                writer.Write(PUZZLE2);
                
                writer.WriteASCIIString(this.AniFile, 256);

                writer.Write(this.Size);

                for (int y = 0; y < this.Size.Height; y++)
                {
                    for (int x = 0; x < this.Size.Width; x++)
                    {
                        writer.Write(this.PuzzleTiles[x, y]);
                    }
                }
                writer.Write(this.RollSpeed);
            }
            //Need to return relative path
            return this.PuzzlePath;
        }

        public void Load()
        {
            if (Path.IsPathFullyQualified(this.PuzzlePath))
                this.PuzzlePath = Path.GetRelativePath(this.ClientPath, this.PuzzlePath);
            string puzzlePath = Path.Combine(this.ClientPath, this.PuzzlePath);

            if (!File.Exists(puzzlePath)) throw new FileNotFoundException($"Puzzle File not found at {puzzlePath}");

            using (BinaryReader br = new BinaryReader(File.OpenRead(puzzlePath)))
            {
                this.Header = br.ReadASCIIString(8);

                this.AniFile = br.ReadASCIIString(256);

                this.Size = new Size(br.ReadUInt32(), br.ReadUInt32());
                this.PuzzleTiles = new ushort[this.Size.Width, this.Size.Height];

                for(int y = 0; y < this.Size.Height; y++)
                {
                    for(int x = 0; x < this.Size.Width; x++)
                    {
                        this.PuzzleTiles[x, y] = br.ReadUInt16();
                    }
                }

                if (this.Header == "PUZZLE")
                    this.RollSpeed = new PixelPosition()
                    {
                        X = 0,
                        Y = 0
                    };
                else
                    this.RollSpeed = br.ReadPixelPosition();

                Console.Write($"Finished reading {puzzlePath} ");
                var consoleColor = Console.BackgroundColor;
                if (br.BaseStream.Position != br.BaseStream.Length)
                    Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine($"{br.BaseStream.Position}/{br.BaseStream.Length}");
                Console.BackgroundColor = consoleColor;
            }
        }

        public int GetWidth()
        {
            if (width != -1)
                return width;

            AniFile aniFile = new AniFile(ClientPath, AniFile);

            if (aniFile.Anis.Count == 0)
            {
                width = 256;
                return width;
            }

            foreach (var file in aniFile.Anis.Values)
            {
                try
                {
                    ClientResources clientResources = new ClientResources(ClientPath);
                    Stream fileStream = clientResources.GetFile(file.Frames.Peek());
                    if (fileStream == null) continue;
                    
                    using SixLabors.ImageSharp.Image<Rgba32> image = DDSConvert.Load(fileStream);

                    return image.Width;
                }
                catch(DirectoryNotFoundException dnfe){}
                catch (FileNotFoundException fnfe){}
            }
            width = 256;
            return width;
        }
    }
}
