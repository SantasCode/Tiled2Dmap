using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Tiled2Dmap.CLI.Preview
{
    public class RenderWindow : Game
    {

        private GraphicsDeviceManager _graphicsDeviceManager;
        public TextureCache TextureCache { get; set; }
        public SpriteBatch RenderSpriteBatch { get; set; }
        public SpriteBatch MouseSpriteBatch { get; set; }
        public Camera2D Camera { get; set; }

        private Render.DmapFileRender _dmapFileRender;
        private Render.PuxFileRender _puxFileRender;
        private string _client;
        private string _dmapFile;
        private Texture2D mouseTexture;

        private int fileIndex = 0;
        private List<string> fileList;

        private KeyboardState oldKeyboardState;

        private int initWindowWidth = 1024;
        private int initWindowHeight = 768;

        public RenderWindow(string client, int windowWidth, int windowHeight, string dmapFile = "")
        {
            _client = client;
            _dmapFile = dmapFile;
            //Create the window

            initWindowWidth = windowWidth;
            initWindowHeight = windowHeight;
            _graphicsDeviceManager = new GraphicsDeviceManager(this)
            {
            /* Bug in 3.8 - fixed in 3.8.1 set in initialize as work around
                PreferredBackBufferWidth = windowWidth,
                PreferredBackBufferHeight = windowHeight
            */
            };
            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += Window_ClientSizeChanged;

            fileList = Directory.EnumerateFiles(Path.Combine(client, "map/map"), "*.*")
                .Where(p => p.ToLower().EndsWith(".dmap") || p.ToLower().EndsWith(".7z") || (p.ToLower()).EndsWith("zmap"))
                .Select(p => Path.GetRelativePath(client, p).ToLower()).ToList();
        }

        private void Window_ClientSizeChanged(object sender, EventArgs e)
        {
            Log.Info($"Client Size Change Rec({Window.ClientBounds.X},{Window.ClientBounds.Y},{Window.ClientBounds.Width},{Window.ClientBounds.Height})");
        }
        protected override void LoadContent()
        {
            RenderSpriteBatch = new(GraphicsDevice);
            MouseSpriteBatch = new(GraphicsDevice);
            TextureCache = new(_client, GraphicsDevice);

            if (Path.GetExtension(_dmapFile.ToLower()) == ".pux")
                SetPux(TextureCache.ClientResources.ClientDirectory, _dmapFile);
            else
                SetDmap(TextureCache.ClientResources.ClientDirectory, _dmapFile);

            mouseTexture = GetColoredSquare(5, Color.Red);
        }

        protected override void Initialize()
        {
            base.Initialize();

            //Bug in 3.8 - fixed in 3.8.1 Setting window size and applying
            _graphicsDeviceManager.PreferredBackBufferWidth = initWindowWidth;
            _graphicsDeviceManager.PreferredBackBufferHeight = initWindowHeight;
            _graphicsDeviceManager.ApplyChanges();

            Camera = new(GraphicsDevice.Viewport);
        }
        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            Camera.UpdateCamera(GraphicsDevice.Viewport);

            if (_dmapFileRender != null)
                _dmapFileRender.Update(gameTime, Camera);

            if (_puxFileRender != null)
                _puxFileRender.Update(gameTime, Camera);

            #region Keyboard Input
            if (Keyboard.GetState().IsKeyDown(Keys.PageUp) && !oldKeyboardState.IsKeyDown(Keys.PageUp))
                SetDmap(TextureCache.ClientResources.ClientDirectory, deltaIdx: -1);
            if (Keyboard.GetState().IsKeyDown(Keys.PageDown) && !oldKeyboardState.IsKeyDown(Keys.PageDown))
                SetDmap(TextureCache.ClientResources.ClientDirectory, deltaIdx: 1);
            oldKeyboardState = Keyboard.GetState();
            #endregion
        }
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Pink);

            RenderSpriteBatch.Begin(blendState: BlendState.NonPremultiplied, transformMatrix:Camera.Transform);
            MouseSpriteBatch.Begin();

            if (_dmapFileRender != null)
                _dmapFileRender.Draw(RenderSpriteBatch, TextureCache, Camera);
            if(_puxFileRender != null)
                _puxFileRender.Draw(RenderSpriteBatch, TextureCache, Camera);

            MouseSpriteBatch.Draw(mouseTexture, Mouse.GetState().Position.ToVector2(), Color.White);

            RenderSpriteBatch.End();
            MouseSpriteBatch.End();
        }
        public void SetPux(string clientDirectroy, string puxPath = "")
        {
            try
            {
                Dmap.PuxFile puxFile = new Dmap.PuxFile(clientDirectroy, puxPath);
                _puxFileRender = new(puxFile, TextureCache);
            }
            catch(Exception e)
            {
                Window.Title = $"{Path.GetFileName(puxPath)} - Error";
                Log.Error(e.ToString());
            }
        }
        public void SetDmap(string clientDirectory, string dmapPath = "", int deltaIdx = 0)
        {
            _dmapFileRender = null;

            //Clear the cache to alleviate resources.
            TextureCache.Clear();
            Camera = new(GraphicsDevice.Viewport);

            int newIdx = 0;
            if (dmapPath.Length > 0)
            {
                newIdx = fileList.IndexOf(Path.GetRelativePath(clientDirectory,dmapPath));

                if (newIdx == -1)
                {
                    Window.Title = $"{Path.GetFileName(dmapPath)} - not found";
                    return;
                }
            }
            else if (deltaIdx != 0)
            {
                newIdx = fileIndex + deltaIdx;
                newIdx %= fileList.Count;
                
                if (newIdx < 0)
                    newIdx = fileList.Count - 1;
            }
            fileIndex = newIdx;

            Window.Title = $"{Path.GetFileName(fileList[fileIndex])}";

            try
            {
                Dmap.DmapFile dmapFile = new Dmap.DmapFile(fileList[fileIndex], clientDirectory);
                _dmapFileRender = new(dmapFile, TextureCache);
            }
            catch (Exception fnfe)
            {
                Window.Title = $"{Path.GetFileName(fileList[fileIndex])} - Error";
                if (fnfe.Message.Contains("PUX"))
                    SetDmap(clientDirectory, deltaIdx: deltaIdx == 0 ? 1 : deltaIdx);
                else
                    Log.Error(fnfe.ToString());
            }

        }

        public Texture2D GetColoredSquare(int size, Color desiredColor)
        {
            Color[] dataColors = new Color[size * size];
            int row = -1; //increased on first iteration to zero!
            int column = 0;
            for (int i = 0; i < dataColors.Length; i++)
            {
                column++;
                if (i % size == 0) //if we reach the right side of the rectangle go to the next row as if we were using a 2D array.
                {
                    row++;
                    column = 0;
                }
                dataColors[i] = desiredColor;

            }
            Texture2D texture = new Texture2D(GraphicsDevice, size, size);
            texture.SetData(0, new Rectangle(0, 0, size, size), dataColors, 0, size * size);
            return texture;
        }
    }
}
