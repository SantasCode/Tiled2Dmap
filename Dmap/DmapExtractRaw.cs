using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tiled2Dmap.CLI.Utility;

namespace Tiled2Dmap.CLI.Dmap
{
    /// <summary>
    /// Extract the Dmap, without renaming any resources.
    /// </summary>
    internal class DmapExtractRaw
    {
        private readonly ILogger<DmapExtractRaw> _logger;
        private readonly ClientResources _clientResources;
        private readonly string _dmapPath;
        private readonly DmapFile _dmap;
        private readonly string _destination;

        private Dictionary<string, AniFile> aniFileCache = new();

        private List<string> _paths = new();

        public DmapExtractRaw(ILogger<DmapExtractRaw> logger, ClientResources clientResources, string mapToExtract, string destination)
        {
            _logger = logger;
            _dmap = new DmapFile(mapToExtract, clientResources.ClientDirectory);
            _dmapPath = mapToExtract;
            _clientResources = clientResources;
            _destination = destination;

            Process();
        }

        private void Process()
        {
            //Copy the dmap document
            Copy(_dmapPath);

            if(_dmap.PuxFile!= null) 
                ProcessPux(_dmap.PuxFile);
            if (_dmap.PuzzleFile != null) 
                ProcessPul(_dmap.PuzzleFile);

            //Copy terrain scenes.
            foreach(var tScene in _dmap.TerrainScenes)
                Copy(tScene.SceneFile);

            //Copy covers;
            foreach (var cover in _dmap.Covers)
                ProcessAni(cover.AniPath, cover.AniName, "Covers");

            //Copy Additional Puzzles.
            foreach(var puzzle in _dmap.Puzzles)
            {
                if (puzzle.EndsWith("pux")) ProcessPux(puzzle);
                else ProcessPul(puzzle);
            }

            //Copy Effects
            //TODO: Load 3deffects.ini to get effect path
            foreach (var effect in _dmap.Effects)
                _logger.LogWarning("Map contains effect {0}, copying effects is not supported yet.", effect.EffectName);

            //Copy Sounds
            foreach(var sound in _dmap.Sounds)
                Copy(sound.SoundFile);

            //Copy EffectNews
            //TODO: Load 3deffects.ini to get effect path
            foreach (var newEffect in _dmap.EffectNews)
                _logger.LogWarning("Map contains effect {0}, copying new effects is not supported yet.", newEffect.EffectName);

            //Copy Unknown1Objs
            foreach (var unk1 in _dmap.Unknown1Objs)
                ProcessAni(unk1.AniFile, unk1.AniName, "Unknown1");

            //Copy Unknown2Objs
            foreach (var unk2 in _dmap.Unknown2Objs)
                Copy(unk2.Name);

            //Copy SceneLayers
            foreach(var layer in _dmap.SceneLayers)
            {
                //Copy terrain scenes.
                foreach (var tScene in layer.TerrainScenes)
                    Copy(tScene.SceneFile);

                //Copy Additional Puzzles.
                foreach (var puzzle in layer.Puzzles)
                {
                    if (puzzle.EndsWith("pux")) ProcessPux(puzzle, "Scene");
                    else ProcessPul(puzzle, "Scene");
                }

                //Copy Effects
                //TODO: Load 3deffects.ini to get effect path
                foreach (var effect in layer.Effects)
                    _logger.LogWarning("Map Scene contains effect {0}, copying effects is not supported yet.", effect.EffectName);
                
                //Copy EffectNews
                //TODO: Load 3deffects.ini to get effect path
                foreach (var newEffect in layer.EffectNews)
                    _logger.LogWarning("Map Scene contains effect {0}, copying new effects is not supported yet.", newEffect.EffectName);

            }
        }

        private void ProcessPul(string filePath, string context = "")
        {
            //Copy the actual pul document
            Copy(filePath);

            PuzzleFile puzzleFile = new PuzzleFile(_clientResources.ClientDirectory, filePath);

            string aniFile = puzzleFile.AniFile;


            //Iterate puzzle pieces and copy.
            for (int xidx = 0; xidx < puzzleFile.Size.Width; xidx++)
            {
                for (int yidx = 0; yidx < puzzleFile.Size.Height; yidx++)
                {
                    ushort puzzleId = puzzleFile.PuzzleTiles[xidx, yidx];

                    //Skip piece if its blank.
                    if (puzzleId == ushort.MaxValue) continue;

                    ProcessAni(aniFile, $"Puzzle{puzzleId}", $"{context} Pul");
                }
            }
        }
        private void ProcessPux(string filePath, string context = "")
        {
            //Copy the actual pux document
            Copy(filePath);

            PuxFile puxFile = new PuxFile(_clientResources.ClientDirectory,filePath);

            foreach (var textGroup in puxFile.TextureGroups.Values) 
            {
                //Copy the ani document
                Copy(textGroup.AniFile);
                ProcessAni(textGroup.AniFile, textGroup.AniName, $"{context} Pux");
            }

            foreach (var edgeGroup in puxFile.EdgeGroups.Values)
            {
                //Copy the ani document
                Copy(edgeGroup.AniFile);
                ProcessAni(edgeGroup.AniFile, edgeGroup.AniName, $"{context} Pux");
            }

        }

        private void ProcessAni(string aniFile, string aniName, string context = "")
        {
            //Copy the actual document
            Copy(aniFile);

            if (!aniFileCache.TryGetValue(aniFile, out AniFile aniDoc)) {
                aniDoc = new AniFile(_clientResources.ClientDirectory, aniFile);
                aniFileCache.Add(aniFile, aniDoc);
            }

            if (aniDoc.Anis.TryGetValue(aniName, out Ani ani))
            {
                foreach(var aniFrame in ani.Frames)
                    Copy(aniFrame);
            }
            else
                _logger.LogError("{0} - Ani File {1} doesn't contain an entry for {2}", context, aniFile, aniName);
        }

        private void Copy(string path)
        {
            if(_paths.Contains(path)) return;

            var fileStream = _clientResources.GetFile(path);

            if(fileStream== null)
            {
                _logger.LogWarning($"Unable to copy {path}, file does not exist in the directory or package", path);
                return;
            }

            string outputPath = Path.Combine(_destination, path);

            if (File.Exists(outputPath))
                return;

            string? outDir = Path.GetDirectoryName(outputPath);
            if (outDir == null) { _logger.LogWarning($"Unable to copy resources to {outputPath}, can't identify directory"); return; }

            if (!Directory.Exists(outDir))
                Directory.CreateDirectory(outDir);

            using var outFile = File.OpenWrite(outputPath);
            
            fileStream.Seek(0, SeekOrigin.Begin);

            fileStream.CopyTo(outFile);
        }
    }
}
