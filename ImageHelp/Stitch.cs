using System;
using Tiled2Dmap.CLI.Dmap;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Tiled2Dmap.CLI.ImageServices
{
    public class Stitch : IDisposable
    {

        public Image<Rgba32> Image { get; private set; } = null;

        public PuzzleFile PuzzleFile { get; init; }

        private Size puzzlePieceSize = new Size(0, 0);

        private readonly Utility.ClientResources _clientResources;

        public Stitch(Utility.ClientResources clientResources, PuzzleFile puzzleFile)
        {
            _clientResources = clientResources;
            PuzzleFile = puzzleFile;
            
            LoadStitch();
        }

        private void LoadStitch()
        {
            //There is only a single ani file, load it.
            AniFile aniFile = new AniFile(this._clientResources.ClientDirectory, this.PuzzleFile.AniFile);

            bool sizeIsSet = false;

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
                    using (Image<Rgba32> pieceImage = DDSConvert.Load(_clientResources.GetFile(puzzlePiecePath)))
                    {
                        if (!sizeIsSet)
                        {
                            puzzlePieceSize = pieceImage.Size();
                            sizeIsSet = true;

                            Image = new(puzzlePieceSize.Width * (int)this.PuzzleFile.Size.Width, puzzlePieceSize.Height * (int)this.PuzzleFile.Size.Height);
                        }
                        Image.Mutate(x =>
                        {
                            x.DrawImage(pieceImage, new SixLabors.ImageSharp.Point(xidx * puzzlePieceSize.Width, yidx * puzzlePieceSize.Height), 1);
                        });
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
            if(Image != null ) Image.Dispose();
        }
    }
}
