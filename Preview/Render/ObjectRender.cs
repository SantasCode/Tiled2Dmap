using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tiled2Dmap.CLI.Preview.Render
{
    public class ObjectRender
    {
        //Orthographic bounds of the object.
        public Rectangle Bounds { get; set; }
        private bool _textureSizeSet = true;

        /// <summary>
        /// Used to determine draw order.
        /// </summary>
        public Vector2 IsometricPosition { get; protected set; }

        public Dictionary<int, int> FrameTextures { get; set; } = new();

        /// <summary>
        /// Number of milliseconds between frames.
        /// </summary>
        public uint AnimationInterval { get; set; } = 0;

        /// <summary>
        /// Number of Pixels to move per second - I think
        /// </summary>
        public Vector2 MoveRate { get; set; } = Vector2.Zero;

        private double _lastAnimationUpdate = 0;
        private int _frame = 0;
        private void UpdateMove(GameTime gameTime)
        {
            //Don't update if the move rate is 0.
            if (MoveRate == Vector2.Zero) return;

            //Determine how much we should move in the elapsed amount of time since last move.
            Vector2 moved = MoveRate * (float)gameTime.ElapsedGameTime.TotalSeconds;

            //Move the bounding rectangle
            Bounds = new()
            {
                X = Bounds.X + (int)moved.X,
                Y = Bounds.Y + (int)moved.Y,
                Width = Bounds.Width,
                Height = Bounds.Height
            };
        }
        private void UpdateFrame(GameTime gameTime, Camera2D camera2D)
        {
            //Don't update frame if not in visible area
            if (!camera2D.VisibleArea.Intersects(Bounds)) return;

            //Don't update frame if there is 1 or less frames, not animated.
            if (FrameTextures.Count < 2) return;

            //Don't update frame if not enough time has lapsed.
            if (_lastAnimationUpdate + (double)AnimationInterval > gameTime.TotalGameTime.TotalMilliseconds) return;

            //Increase the frame by one rolling over when we exceed the number of frames.
            _frame = (_frame + 1) % FrameTextures.Count;
            _lastAnimationUpdate = gameTime.TotalGameTime.TotalMilliseconds;
        }
        public void Update(GameTime gameTime, Camera2D camera2D)
        {
            UpdateFrame(gameTime, camera2D);
            UpdateMove(gameTime);
        }
        public void Draw(SpriteBatch spriteBatch, TextureCache textureCache, Camera2D camera2D)
        {
            //If there are no textures, can't draw.
            if (FrameTextures.Count < 1) return;

            //Don't draw frame if not in visible area
            if (!camera2D.VisibleArea.Intersects(Bounds)) return;

            Texture2D texture2D = textureCache.GetTexture2D(FrameTextures[_frame]);

            //Don't want to draw the texture if its null
            if (texture2D == null) return;

            //Set the bounds to the accurate size if it hasn't been set already.
            if(!_textureSizeSet)
            {
                Bounds = new()
                {
                    X = Bounds.X,
                    Y = Bounds.Y,
                    Width = texture2D.Width,
                    Height = texture2D.Height
                };
            }

            //Draw the object without any tinting (color white)
            spriteBatch.Draw(texture2D, Bounds.Location.ToVector2(), Color.White);
        }
    }
}
