using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tiled2Dmap.CLI.Preview
{
    public class TextureCache
    {
        public Utility.ClientResources ClientResources { get; init; }
        public Dictionary<int, Texture2D> Textures { get; init; } = new();
        
        private Dictionary<string, int> TextureIds = new();
        private Dictionary<int, string> TexturePaths = new();

        private GraphicsDevice _grahicDevice;
        
        public TextureCache(string client, GraphicsDevice graphicsDevice) 
        {
            ClientResources = new(client);
            _grahicDevice = graphicsDevice;
        }
        /// <summary>
        /// Load the texture, if it hasn't been, and return.
        /// </summary>
        /// <param name="textureId"></param>
        /// <returns></returns>
        public Texture2D GetTexture2D(int textureId)
        {
            //Return the texture if it has already been loaded.
            if (Textures.TryGetValue(textureId, out Texture2D texture2D))
                return texture2D;

            //Log.Info($"Loading asset texture {TexturePaths[textureId]}");

            //Load the asset
            Stream file = null;
            string texturePath = TexturePaths[textureId];
            try
            {
                if (texturePath.ToLower().EndsWith(".msk"))
                {
                    //Try gettings the associated dds
                    texturePath = texturePath.Replace(".msk", ".dds", ignoreCase: true, null);
                    file = ClientResources.GetFile(texturePath);
                    //If the dds file did not exist, try tga
                    if (file == null)
                    {
                        texturePath = texturePath.Replace(".dds", ".tga");
                        file = ClientResources.GetFile(texturePath.Replace(".msk", ".tga"));
                    }
                }
                else
                    file = ClientResources.GetFile(texturePath);
            }
            catch(FileNotFoundException fnfe) 
            {
                //Log.Warn($"Could not find {texturePath}");
                return null; 
            }
            //Texture was not found
            if (file == null)
            {
                Textures.Add(textureId, null);
                return null;
            }

            //Get file type from path
            string fileExtension = Path.GetExtension(texturePath);

            //temp - save texture
            //ImageServices.DDSConvert.StreamToPng(file).Save($"C:/Temp/2014holiday/{textureId}.png");
            
            //Texture was found, convert it to a texture. 
            Texture2D newTexture = null;
            switch (fileExtension)
            {
                case ".dds": DDSLib.DDSFromStream(file, _grahicDevice, 0, true, out newTexture); break;
                case ".tga": newTexture = Texture2D.FromStream(_grahicDevice, file); break;
                default: Log.Error($"Texture Cache - Unsupported file type {fileExtension}"); break;
            }

            Textures.Add(textureId, newTexture);

            return newTexture;
        }
        /// <summary>
        /// Add a texture to the cache, this does not load the texture.
        /// </summary>
        /// <param name="relativePath"></param>
        /// <returns></returns>
        public int AddTexture(string relativePath)
        {
            //Return the texture Id if it has already been added.
            if (TextureIds.TryGetValue(relativePath, out int textureId))
                return textureId;

            int newId = TextureIds.Count;
            try
            {
                TextureIds.Add(relativePath, newId);
                TexturePaths.Add(newId, relativePath);
            }
            catch
            {
                //Failed to add texture to both collections, undo
                TextureIds.Remove(relativePath);
                TexturePaths.Remove(newId);
                return -1;
            }

            return newId;
        }

        public void Clear()
        {
            foreach (var texture in Textures.Values)
                texture?.Dispose();
            Textures.Clear();
            TextureIds.Clear();
            TexturePaths.Clear();
        }
    }
}
