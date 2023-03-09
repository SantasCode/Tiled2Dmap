using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Tiled2Dmap.CLI.Extensions;
using Tiled2Dmap.CLI.Utility;
using Tiled2Dmap.CLI.Dmap.Pux;
using System.Text.RegularExpressions;

namespace Tiled2Dmap.CLI.Dmap
{
    public class PuxFile
    {
        public string PuzzleType { get; set; }
        public Dictionary<int, TextureGroup> TextureGroups { get; set; } = new();
        public Dictionary<int, TextureGroup> EdgeGroups { get; set; } = new();
        public Dictionary<int, PuxPiece>[,] PuxPieces { get; set; }
        public Size Size { get; set; }
        
        public string PuzzlePath { get; set; }

        private string ClientPath;
        private int width = -1;

        public PuxFile(string PuzzlePath){ this.PuzzlePath = PuzzlePath; }
        public PuxFile(string ClientPath, string PuzzlePath)
        {
            this.PuzzlePath = PuzzlePath;
            this.ClientPath = ClientPath;
            this.Load();
        }

        public string Save(string ProjectDirectory)
        {
            if (!Directory.Exists(Path.Combine(ProjectDirectory, "map/PuzleSaves")))
                Directory.CreateDirectory(Path.Combine(ProjectDirectory, "map/PuzleSaves"));

            using (BinaryWriter writer = new BinaryWriter(File.OpenWrite(Path.Combine(ProjectDirectory, PuzzlePath))))
            {
            }
            //Need to return relative path
            return this.PuzzlePath;
        }
        public void Load(Stream stream)
        {
            using (BinaryReader br = new BinaryReader(stream))
            {
                this.PuzzleType = br.ReadASCIIString(16);
                //wr.WriteLine(PuzzleType);
                uint token1000 = br.ReadUInt32();//1000

                if (token1000 != 1000) Log.Info("Unknown Token in .pux file is not 1000");

                Size = br.ReadSize();

                PuxPieces = new Dictionary<int, PuxPiece>[Size.Width, Size.Height];

                //CAoxPuzzle::LoadTextureGroup
                uint unk5 = br.ReadUInt32();//1000

                ushort numTextureGroups = br.ReadUInt16();
                for (int i = 0; i < numTextureGroups; i++)
                {
                    byte[] unkBytes = br.ReadBytes(br.ReadUInt16());

                    string aniFile = br.ReadASCIIString(br.ReadUInt16());

                    string puzPiece = br.ReadASCIIString(br.ReadUInt16());

                    uint unk6 = br.ReadUInt32();
                    uint unk7 = br.ReadUInt32();
                    uint unk8 = br.ReadUInt32();
                    uint unk9 = br.ReadUInt32();
                    uint max = br.ReadUInt32();

                    //Determine puzzleId
                    int puzzleId = int.Parse(Regex.Match(puzPiece, @"\d+").Value);
                    TextureGroups.Add(puzzleId, new()
                    {
                        UnknownBytes = unkBytes,
                        AniFile = aniFile,
                        AniName = puzPiece,
                        unk1 = unk6,
                        unk2 = unk7,
                        unk3 = unk8,
                        unk4 = unk9,
                        Max = max
                    });
                    //wr.WriteLine("Texture Group: {0} AniFile: {1}, PuzzleEntry: {2}, {3}, {4}, {5}, {6}, {7}", unkBytes.GetString(), aniFile, puzPiece, unk6, unk7, unk8, unk9, max);

                }
                //End LoadtextureGroup

                //CAoxPuzzle::LoadEdgeGroup
                uint unk10 = br.ReadUInt32();//1000
                ushort numEdgeGroups = br.ReadUInt16();
                for (int i = 0; i < numEdgeGroups; i++)
                {
                    byte[] unkBytes = br.ReadBytes(br.ReadUInt16()); //A string in simplified chinese. Appears to be unused in client.

                    string aniFile = br.ReadASCIIString(br.ReadUInt16());

                    string puzPiece = br.ReadASCIIString(br.ReadUInt16());

                    uint unk6 = br.ReadUInt32();
                    uint unk7 = br.ReadUInt32();
                    uint unk8 = br.ReadUInt32();
                    uint unk9 = br.ReadUInt32();
                    uint max = br.ReadUInt32();

                    //Determine puzzleId
                    int puzzleId = int.Parse(Regex.Match(puzPiece, @"\d+").Value);
                    EdgeGroups.Add(puzzleId, new()
                    {
                        UnknownBytes = unkBytes,
                        AniFile = aniFile,
                        AniName = puzPiece,
                        unk1 = unk6,
                        unk2 = unk7,
                        unk3 = unk8,
                        unk4 = unk9,
                        Max = max
                    });
                    //wr.WriteLine("Edge Group: {0} AniFile: {1}, PuzzleEntry: {2}, {3}, {4}, {5}, {6}, {7}", unkBytes.GetString(), aniFile, puzPiece, unk6, unk7, unk8, unk9, max);
                }
                //End LoadEdgeGroup
                //PuzzleUnitData
                uint puzzGridCnt = br.ReadUInt32();

                //I assume its goin left to right top to bottom
                for (int i = 0; i < puzzGridCnt; i++)
                {
                    int xidx = (int)(i % Size.Width);
                    int yidx = (int)(i / Size.Width);
                    byte dataCnt = br.ReadByte();
                    if (dataCnt != 0)
                    {
                        PuxPieces[xidx, yidx] = new();
                        for (int j = 0; j < dataCnt; j++)
                        {
                            PuxPieces[xidx, yidx].Add(j, new()
                            {
                                AniID = br.ReadUInt16(),
                                Unknown = br.ReadInt32()
                            });
                        }
                    }
                    else
                        PuxPieces[xidx, yidx] = null;
                }

                uint numEdgeData = br.ReadUInt32();
               // wr.WriteLine("EdgeLayer Size: {0}", numEdgeData);
                for (int i = 0; i < numEdgeData; i++)
                {
                    uint unk = br.ReadUInt32();
                    byte[] unkBytes = br.ReadBytes(4);//Read as four individuals bytes. Left,Top,Right,  Down  perhaps?
                }
                Log.Info("Finished reading pux");
            }
        }
        public void Load()
        {
            if (Path.IsPathFullyQualified(this.PuzzlePath))
                this.PuzzlePath = Path.GetRelativePath(this.ClientPath, this.PuzzlePath);
            string puzzlePath = Path.Combine(this.ClientPath, this.PuzzlePath);

            if (!File.Exists(puzzlePath)) throw new FileNotFoundException($"Pux File not found at {puzzlePath}");

            Load(File.OpenRead(puzzlePath));
        }
    }
}
