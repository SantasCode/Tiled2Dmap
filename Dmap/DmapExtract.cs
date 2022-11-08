using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tiled2Dmap.CLI.Utility;
#nullable enable
namespace Tiled2Dmap.CLI.Dmap
{
    internal class DmapExtract
    {
        private readonly ClientResources _clientResources;
        private readonly string _dmapName;
        private readonly DmapFile _dmapToExtract;
        
        private Dictionary<string, AniFile> aniFileCache = new();

        private string outputDirectory;

        public DmapExtract(ClientResources clientResources, DmapFile dmapToExtract, string MapName)
        {
            _dmapName = MapName;
            _clientResources = clientResources;
            _dmapToExtract = dmapToExtract;
        }
        private AniFile GetAniFile(string AniPath)
        {
            AniFile toRet;
            if (!aniFileCache.TryGetValue(AniPath, out toRet))
            {
                toRet = new AniFile(_clientResources.ClientDirectory, AniPath);

                if (toRet != null)
                    aniFileCache.Add(AniPath, toRet);
            }
            return toRet;
        }
        private Ani CopyAni(Ani toCopy, string? AniName = null)
        {
            Ani newAni = new()
            {
                Name = AniName ?? toCopy.Name
            };
            foreach(var frame in toCopy.Frames)
            {
                CopyResources(frame);
                newAni.Frames.Enqueue(frame);
            }
            return newAni;
        }
        private void CopyResources(string path)
        {
            string outputPath = Path.Combine(outputDirectory, path);

            if (File.Exists(outputPath))
                return;

            string? outDir = Path.GetDirectoryName(outputPath);
            if(outDir == null) { Log.Warn($"Unable to copy resources to {outputPath}, can't retrieve directory"); return; }

            if(!Directory.Exists(outDir))
                Directory.CreateDirectory(outDir);


            string inputPath = Path.Combine(_clientResources.ClientDirectory, path);
            if (!File.Exists(inputPath))
            {
                Log.Warn($"Unable to copy {inputPath}, file does not exist");
                return;
            }

            File.Copy(inputPath, outputPath);
        }

        private void ExtractPuzzle(string puzzlePath, AniFile newAniFile, string? puzzleName = null)
        {
            PuzzleFile puzzleFile = new PuzzleFile(_clientResources.ClientDirectory, puzzlePath);
            puzzleFile.PuzzlePath = puzzleName ?? puzzleFile.PuzzlePath;
            //Load the existing aniFile before replacing it.
            AniFile existingAni = GetAniFile(puzzleFile.AniFile);

            puzzleFile.AniFile = newAniFile.AniFilePath;//Set ani file to our newly created ani file.
            //Save puzzle file with new anifile path
            puzzleFile.Save(outputDirectory);

            //Iterate puzzle pieces and copy.
            for (int xidx = 0; xidx < puzzleFile.Size.Width; xidx++)
            {
                for (int yidx = 0; yidx < puzzleFile.Size.Height; yidx++)
                {
                    ushort puzzleId = puzzleFile.PuzzleTiles[xidx, yidx];

                    //Skip piece if its blank.
                    if (puzzleId == ushort.MaxValue) continue;

                    //Check if we've already copied this puzzle piece
                    if (newAniFile.Anis.ContainsKey($"Puzzle{puzzleId}")) continue;

                    Ani eAni = existingAni.Anis[$"Puzzle{puzzleId}"];
                    newAniFile.Anis.Add(eAni.Name, CopyAni(eAni));

                }
            }
        }
        private void ExtractTerrainScene(string scenePath, AniFile newSceneAni, string? sceneName = null)
        {
            SceneFile oldScene = new SceneFile(_clientResources.ClientDirectory, scenePath);
            oldScene.SceneFilePath = sceneName ?? oldScene.SceneFilePath;

            foreach (var piece in oldScene.SceneParts)
            {
                AniFile sceneExistingAni = GetAniFile(piece.AniPath);

                if (sceneExistingAni.Anis.TryGetValue(piece.AniName, out var val))
                {
                    piece.AniPath = newSceneAni.AniFilePath;
                    if (!newSceneAni.Anis.ContainsKey(val.Name))
                    {
                        newSceneAni.Anis.Add(val.Name, CopyAni(val));
                    }
                }
                else
                    Log.Warn($"Unable to get scene Ani {piece.AniPath} - {piece.AniName}");
            }
            oldScene.Save(outputDirectory);
        }
        public void Extract(string OutputDirectory)
        {
            outputDirectory = OutputDirectory;

            _dmapToExtract.DmapName = _dmapName;
            _dmapToExtract.DmapPath = $"map/map/{_dmapToExtract.DmapName}.dmap";

            AniFile mapAni = new AniFile($"ani/{_dmapToExtract.DmapName}.ani");

            #region PuzzleBackground
            string puzzRelPath = $"map/puzzle/{_dmapToExtract.DmapName}.pul";
            ExtractPuzzle(_dmapToExtract.PuzzleFile, mapAni, puzzRelPath);
            _dmapToExtract.PuzzleFile = puzzRelPath;
            #endregion

            #region Scene
            int sceneCnt = 0;
            foreach (var scene in _dmapToExtract.TerrainScenes)
            {
                //Create a new ani foreach Scene
                AniFile newSceneAni = new AniFile($"ani/{_dmapName}_s{sceneCnt}.ani");
                ExtractTerrainScene(scene.SceneFile, newSceneAni, $"ani/{_dmapName}_s{sceneCnt}.scene");
                newSceneAni.Save(outputDirectory);
                sceneCnt++;
            }
            #endregion

            #region Cover
            foreach (var cover in _dmapToExtract.Covers)
            {
                AniFile coverAniFile = GetAniFile(cover.AniPath);
                cover.AniPath = mapAni.AniFilePath;

                if (coverAniFile.Anis.TryGetValue(cover.AniName, out var coverAni))
                {
                    if (!mapAni.Anis.ContainsKey(cover.AniName))
                    {
                        mapAni.Anis.Add(cover.AniName, CopyAni(coverAni));
                    }
                }
                else
                {
                    Log.Warn($"Unable to get scene Ani {cover.AniPath} - {cover.AniName}");
                }
            }
            #endregion

            #region AdditionalPuzzle
            int puzzleCnt = 0;
            foreach (var puzzle in _dmapToExtract.Puzzles)
            {
                AniFile newPuzzleFile = new AniFile($"ani/{_dmapToExtract.DmapName}_p{puzzleCnt}.ani");
                ExtractPuzzle(puzzle, newPuzzleFile, $"map/puzzle/{_dmapToExtract.DmapName}_p{puzzleCnt}.pul");
                newPuzzleFile.Save(outputDirectory);
                puzzleCnt++;
            }
            #endregion

            #region Effects
            foreach (var effect in _dmapToExtract.Effects)
                Log.Warn($"NOT COPYING EFFECT: {effect.EffectName}");
            #endregion

            #region Sound
            foreach (var sound in _dmapToExtract.Sounds)
            {
                CopyResources(sound.SoundFile);
            }
            #endregion

            #region SceneLayers
            foreach(var layer in _dmapToExtract.SceneLayers)
            {
                foreach(var scene in layer.TerrainScenes)
                {
                    AniFile newSceneAni = new AniFile($"ani/{_dmapName}_s{sceneCnt}.ani");
                    ExtractTerrainScene(scene.SceneFile, newSceneAni, $"ani/{_dmapName}_s{sceneCnt}.scene");
                    newSceneAni.Save(outputDirectory);
                    sceneCnt++;
                }
                foreach (var puzzle in layer.Puzzles)
                {
                    AniFile newPuzzleFile = new AniFile($"ani/{_dmapToExtract.DmapName}_p{puzzleCnt}.ani");
                    ExtractPuzzle(puzzle, newPuzzleFile, $"map/puzzle/{_dmapToExtract.DmapName}_p{puzzleCnt}.pul");
                    newPuzzleFile.Save(outputDirectory);
                    puzzleCnt++;
                }
            }
            #endregion

            //Save dmap instead of copy to ensure format is supported.
            _dmapToExtract.Save(OutputDirectory);

            //Save the ani file after everything is assembled.
            mapAni.Save(OutputDirectory);
        }
    }
}