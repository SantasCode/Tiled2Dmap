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
    public class DmapFileRender
    {
        private List<ObjectRender> _background = new();
        private List<ObjectRender> _scene = new();
        private List<ObjectRender> _cover = new();
        
        //Setup a cache so we don't reload anifiles
        private Dictionary<string, AniFile> _aniCache = new();



        public DmapFileRender(DmapFile dmapFile, TextureCache textureCache)
        {

            #region Background
            //Construct a Renderable data structure from a given dmapFile
            PuzzleFile puzzleFile = new PuzzleFile(textureCache.ClientResources.ClientDirectory, dmapFile.PuzzleFile);

            //Need to determine the size of puzzle pieces
            int puzzleWidth = puzzleFile.GetWidth();

            _aniCache.Add(puzzleFile.AniFile, new AniFile(textureCache.ClientResources.ClientDirectory, puzzleFile.AniFile));

            for(int xidx = 0; xidx < puzzleFile.Size.Width; xidx++)
                for(int yidx = 0; yidx < puzzleFile.Size.Height; yidx++)
                {
                    ObjectRender objectRender = new();

                    //Add each frame to the cache and assign it to the object.
                    LoadAniFrames(puzzleFile.AniFile, $"Puzzle{puzzleFile.PuzzleTiles[xidx, yidx]}", textureCache, ref objectRender);

                    //TODO: Determine animation rate when a puzzle has multiple frames.

                    objectRender.Bounds = new()
                    {
                        X = xidx * puzzleWidth,
                        Y = yidx * puzzleWidth,
                        Width = puzzleWidth,
                        Height = puzzleWidth
                    };
                    _background.Add(objectRender);
                }
            #endregion Background

            //Create a coordinate converter to help place objects within puzzle coordinate space.
            CoordConverter coordConverter = new(new System.Drawing.Size((int)dmapFile.SizeTiles.Width, (int)dmapFile.SizeTiles.Height), 
                new System.Drawing.Size((int)puzzleFile.Size.Width * puzzleFile.GetWidth(), (int)puzzleFile.Size.Height * puzzleFile.GetWidth()));


            #region Covers
            //Sort covers before creating map render objects.
            dmapFile.Covers.Sort(CoverDrawOrder);

            foreach(var cover in dmapFile.Covers)
            {
                ObjectRender objectRender = new();
                LoadAniFrames(cover.AniPath, cover.AniName, textureCache, ref objectRender);

                //Convert the isometric tile to orthographic World coords
                var bgPos = coordConverter.Cell2Bg(new System.Drawing.Point((int)cover.Position.X, (int)cover.Position.Y));

                //Add the orthographic offset
                bgPos.X -= cover.Offset.X;
                bgPos.Y -= cover.Offset.Y;

                objectRender.Bounds = new()
                {
                    X = bgPos.X,
                    Y = bgPos.Y,
                    Width = 512,
                    Height = 512
                };

                objectRender.AnimationInterval = cover.AnimationInterval;
                _cover.Add(objectRender);
            }
            #endregion

            #region Scene
            foreach(var scene in dmapFile.TerrainScenes)
            {
                SceneFile sceneFile = new(textureCache.ClientResources.ClientDirectory, scene.SceneFile);
                
                //Sort the scene parts before adding render objects.
                sceneFile.SceneParts.Sort(SceneDrawOrder);

                foreach(var spart in sceneFile.SceneParts)
                {
                    ObjectRender objectRender = new();
                    LoadAniFrames(spart.AniPath, spart.AniName, textureCache, ref objectRender);

                    var bgPos = coordConverter.Cell2Bg(new System.Drawing.Point((int)scene.Position.X + spart.TileOffset.X, (int)scene.Position.Y + spart.TileOffset.Y));
                    bgPos.X += spart.PixelLocation.X;
                    bgPos.Y += spart.PixelLocation.Y;

                    objectRender.Bounds = new()
                    {
                        X = bgPos.X,
                        Y = bgPos.Y,
                        Width = 512,
                        Height = 512
                    };

                    objectRender.AnimationInterval = spart.Interval;
                    _scene.Add(objectRender);
                }
            }
            #endregion
        }
        private static int SceneDrawOrder(ScenePart x, ScenePart y)
        {
            //Taken from soul source 2DMapObj.cpp AddObj

            //Return -1 if cover1 should be before cover2
            if ((x.TileOffset.X - x.Size.Width + 1) - y.TileOffset.X > 0 ||
                (y.TileOffset.X - y.Size.Width + 1) - x.TileOffset.X > 0)
            {
                //No overlay x
                int nNewData = (int)(x.TileOffset.Y - x.Size.Height + 1);
                int nOldData = (int)(y.TileOffset.Y - y.Size.Height + 1);
                if (nNewData > y.TileOffset.Y || nOldData > x.TileOffset.Y)
                {
                    //no y overlay
                    if (x.TileOffset.X + x.TileOffset.Y < y.TileOffset.X + y.TileOffset.Y)
                        return -1; //Insert x before y
                    return 1;//x comes after y
                }
                else
                {
                    //There is y overlay
                    if (x.TileOffset.X < y.TileOffset.X)
                        return -1;//Insert x before y
                    return 1;//x comes after y
                }
            }
            else
            {
                //There is X overlay
                if (x.TileOffset.Y < y.TileOffset.Y)
                    return -1;//Insert x before y
                return 1;//x comes after y
            }
        }
        private static int CoverDrawOrder(Cover x, Cover y)
        {
            //Taken from soul source 2DMapObj.cpp AddObj

            //Return -1 if cover1 should be before cover2
            if((x.Position.X - x.BaseSize.Width + 1) - y.Position.X > 0 ||
                (y.Position.X - y.BaseSize.Width + 1) - x.Position.X > 0)
            {
                //No overlay x
                int nNewData = (int)(x.Position.Y - x.BaseSize.Height + 1);
                int nOldData = (int)(y.Position.Y - y.BaseSize.Height + 1);
                if(nNewData > y.Position.Y || nOldData > x.Position.Y)
                {
                    //no y overlay
                    if(x.Position.X + x.Position.Y < y.Position.X + y.Position.Y)
                        return -1; //Insert x before y
                    return 1;//x comes after y
                }
                else
                {
                    //There is y overlay
                    if(x.Position.X < y.Position.X)
                        return -1;//Insert x before y
                    return 1;//x comes after y
                }
            }
            else
            {
                //There is X overlay
                if(x.Position.Y < y.Position.Y)
                    return -1;//Insert x before y
                return 1;//x comes after y
            }
        }
        private void LoadAniFrames(string aniPath, string aniName, TextureCache textureCache, ref ObjectRender objectRender)
        {
            AniFile aniFile = null;
            if(!_aniCache.TryGetValue(aniPath, out aniFile))
            {
                aniFile = new AniFile(textureCache.ClientResources.ClientDirectory, aniPath);
                _aniCache.TryAdd(aniPath, aniFile);
            }
            if (!aniFile.Anis.ContainsKey(aniName))
            {
                if(!aniName.EndsWith("65535"))
                    Log.Warn($"Ani not found {aniName}");
                return;
            }
            int frameIdx = 0;
            foreach (var frame in aniFile.Anis[aniName].Frames)
                objectRender.FrameTextures.Add(frameIdx++, textureCache.AddTexture(frame));
        }

        public void Update(GameTime gameTime, Camera2D camera2D)
        {
            foreach (var obj in _background)
                obj.Update(gameTime, camera2D);
            foreach (var obj in _scene)
                obj.Update(gameTime, camera2D);
            foreach (var obj in _cover)
                obj.Update(gameTime, camera2D);
        }
        public void Draw(SpriteBatch spriteBatch, TextureCache textureCache, Camera2D camera2D)
        {
            //First draw the background becuase everything is on top of this.
            foreach (var obj in _background)
                obj.Draw(spriteBatch, textureCache, camera2D);

            foreach (var obj in _scene)
                obj.Draw(spriteBatch, textureCache, camera2D);
            
            //Player would be drawn at this point

            //Covers are drawn last because they can cover up scenes/effects/players
            foreach (var obj in _cover)
                obj.Draw(spriteBatch, textureCache, camera2D);
        }
    }
}
