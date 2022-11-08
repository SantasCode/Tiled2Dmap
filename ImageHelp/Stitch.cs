using System;
using System.Drawing;
using Tiled2Dmap.CLI.Dmap;
using System.IO;

namespace Tiled2Dmap.CLI.ImageServices
{
    public class Stitch : IDisposable
    {
        public Bitmap Image { get => _image; }
        private Bitmap _image;

        private Graphics _graphic;

        public PuzzleFile PuzzleFile { get; init; }

        public Size PuzzlePieceSize { get; private set; }

        private Utility.ClientResources ClientResources;

        public Stitch(Utility.ClientResources ClientResources, PuzzleFile PuzzleFile)
        {
            this.ClientResources = ClientResources;
            this.PuzzleFile = PuzzleFile;
            this.LoadStitch();
        }

        private void LoadStitch()
        {
            //There is only a single ani file, load it.
            AniFile aniFile = new AniFile(this.ClientResources.ClientDirectory, this.PuzzleFile.AniFile);

            bool SizeIsSet = false;

            int currentProgress = 0;
            Console.Write($"Stitching Puzzle File...{currentProgress:000}%");
            int expectedSlicedTiles = (int)this.PuzzleFile.Size.Width * (int)this.PuzzleFile.Size.Height;

            for (int xidx = 0; xidx < this.PuzzleFile.Size.Width; xidx++)
            {
                for (int yidx = 0; yidx < this.PuzzleFile.Size.Height; yidx++)
                {
                    //TODO: Cache puzzle pieces to reduce file reads.
                    ushort puzzleId = this.PuzzleFile.PuzzleTiles[xidx, yidx];

                    //Skip piece if its blank.
                    if (puzzleId == ushort.MaxValue) continue;

                    var puzzlePieceFrames = aniFile.Anis[$"Puzzle{puzzleId}"].Frames;

                    if (puzzlePieceFrames.Count > 1)
                        Console.WriteLine($"Warning: More than one frame in puzzle piece {aniFile.AniFilePath} - Puzzle{puzzleId}. Only stitching first frame.");

                    string puzzlePiecePath = puzzlePieceFrames.Peek().Trim();

                    using (Bitmap pieceBitmap = (Path.GetExtension(puzzlePiecePath) == ".dds" ?
                        DDSConvert.StreamToPng(this.ClientResources.GetFile(puzzlePiecePath)) :
                        new Bitmap(this.ClientResources.GetFile(puzzlePiecePath))))
                    {
                        if (!SizeIsSet)
                        {
                            this.PuzzlePieceSize = pieceBitmap.Size;
                            SizeIsSet = true;
                            _image = new Bitmap(this.PuzzlePieceSize.Width * (int)this.PuzzleFile.Size.Width, this.PuzzlePieceSize.Height * (int)this.PuzzleFile.Size.Height);

                            _graphic = Graphics.FromImage(_image);
                            }

                        _graphic.DrawImage(pieceBitmap, xidx * this.PuzzlePieceSize.Width, yidx * this.PuzzlePieceSize.Height);
                    }

                    int progress = (((int)this.PuzzleFile.Size.Height * xidx + yidx) * 100) / expectedSlicedTiles;
                    if (progress > currentProgress)
                    {
                        currentProgress = progress;
                        Console.Write($"\rStitching Puzzle File...{currentProgress:000}%");
                    }
                }
            }
            Console.WriteLine($"\rStitching Puzzle File...100%");
            Console.WriteLine($"Finished Stiching {this.PuzzleFile.PuzzlePath}");
        }
        public void Dispose()
        {
            if(_image != null ) _image.Dispose();
            if (_graphic != null) _graphic.Dispose();
        }
    }
}
