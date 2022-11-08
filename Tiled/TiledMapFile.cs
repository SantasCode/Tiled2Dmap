using System;
using System.Collections.Generic;
using ImageMagick;
using System.Text.Json.Serialization;
using Tiled2Dmap.CLI.Dmap;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Tiled2Dmap.CLI.Tiled
{
    public class TiledMapFile
    {
        #region Properties
        [JsonPropertyName("width")]
        public int WidthTiles { get; set; }
        [JsonPropertyName("height")]
        public int HeightTiles { get; set; }
        public int NextLayerId { get; set; } = 1;
        public int NextObjectId { get; set; } = 1;
        public int TileWidth { get; set; }
        public int TileHeight { get; set; }
        public List<TiledLayer> Layers { get; set; } = new();
        public List<InternalTileSet> TileSets { get; set; } = new();

        public bool Infinite { get; set; } = false;
        public string Orientation { get; set; } = "isometric";
        public string RenderOrder { get; set; } = "right-down";
        public string TiledVersion { get; set; } = "1.7.2";
        public string Type { get; init; } = "map";
        #endregion

        private Dictionary<string, TileSetFile> _TileSetFiles = new();
        private bool _TileSetsLoaded = false;

        public TiledMapFile() { }

        public TiledTile GetTile(string TiledDirectory, int TileId, JsonSerializerOptions JSONOptions)
        {
            //Select the correct tileset based on GId and tileset ffirst gid.
            InternalTileSet intTileSet = null;
            foreach(var tileset in TileSets.OrderBy(p=> p.FirstGId))
            {
                if (TileId >= tileset.FirstGId)
                    intTileSet = tileset;
                else
                    break;
            }
            TileSetFile tileSetFile = null;
            if (!_TileSetFiles.TryGetValue(intTileSet.Source, out tileSetFile))
            {
                tileSetFile = JsonSerializer.Deserialize<TileSetFile>(File.ReadAllText(Path.Combine(TiledDirectory, intTileSet.Source)), JSONOptions);
                _TileSetFiles.Add(intTileSet.Source, tileSetFile);
            }

            TileId -= intTileSet.FirstGId;


            return tileSetFile.Tiles[TileId];
        }
        public TiledLayer GetLayer(string Name)
        {
            return Layers.Where(p => p.Name == Name).FirstOrDefault();
        }
        public InternalTileSet GetInternalTileSet(string Name)
        {
            return TileSets.Where(p => p.Source == Name).FirstOrDefault();
        }
    }
}
