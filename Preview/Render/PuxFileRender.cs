using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tiled2Dmap.CLI.Dmap;


namespace Tiled2Dmap.CLI.Preview.Render
{
    public class PuxFileRender
    {
        private List<ObjectRender> _background = new();

        //Setup a cache so we don't reload anifiles
        private Dictionary<string, AniFile> _aniCache = new();

        public PuxFileRender(PuxFile puxFile, TextureCache textureCache)
        {

            #region Background
            //Construct a Renderable data structure from a given dmapFile
            PuxFile puzzleFile = new PuxFile(textureCache.ClientResources.ClientDirectory, puxFile.PuzzlePath);

            //Need to determine the size of puzzle pieces
            int puzzleWidth = 128;

            for (int xidx = 0; xidx < puzzleFile.Size.Width; xidx++)
                for (int yidx = 0; yidx < puzzleFile.Size.Height; yidx++)
                {
                    ObjectRender objectRender = new();

                    //Get the dictionary of frames that make up this piece of the puzzle.
                    var frames = puzzleFile.PuxPieces[xidx, yidx];

                    //If frame is null, add no object
                    if(frames == null) continue;

                    //If there are no frames, add no object.
                    if (frames.Count < 1) continue;

                    //we are only going to add the single layer at this time.
                    var frame = frames[0];

                    string puzzleId = $"Puzzle{frame.AniID}";

                    if (puzzleFile.TextureGroups.TryGetValue(frame.AniID, out var texture))
                    {
                        LoadAniFrames(texture.AniFile, texture.AniName, textureCache, ref objectRender);
                        
                        objectRender.Bounds = new()
                        {
                            X = xidx * puzzleWidth,
                            Y = yidx * puzzleWidth,
                            Width = puzzleWidth,
                            Height = puzzleWidth
                        };
                        _background.Add(objectRender);
                    }
                    else
                        continue;

                }
            #endregion Background
        }
        public void Update(GameTime gameTime, Camera2D camera2D)
        {
            foreach (var obj in _background)
                obj.Update(gameTime, camera2D);
        }
        public void Draw(SpriteBatch spriteBatch, TextureCache textureCache, Camera2D camera2D)
        {
            //First draw the background becuase everything is on top of this.
            foreach (var obj in _background)
                obj.Draw(spriteBatch, textureCache, camera2D);
        }
        private void LoadAniFrames(string aniPath, string aniName, TextureCache textureCache, ref ObjectRender objectRender)
        {
            AniFile aniFile = null;
            if (!_aniCache.TryGetValue(aniPath, out aniFile))
            {
                aniFile = new AniFile(textureCache.ClientResources.ClientDirectory, aniPath);
                _aniCache.TryAdd(aniPath, aniFile);
            }
            if (!aniFile.Anis.ContainsKey(aniName))
            {
                if (!aniName.EndsWith("65535"))
                    Log.Warn($"Ani not found {aniName}");
                return;
            }
            int frameIdx = 0;
            foreach (var frame in aniFile.Anis[aniName].Frames)
                objectRender.FrameTextures.Add(frameIdx++, textureCache.AddTexture(frame));
        }
    }
}
