using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tiled2Dmap.CLI.Dmap;
using Tiled2Dmap.CLI.Utility;

namespace Tiled2Dmap.CLI.Dmap.Json
{
    internal class JsonDmapFile
    {
        public byte[] Header { get; set; }
        public uint MapVersion { get; set; }
        public string PuzzleFile { get; set; }
        public Size SizeTiles { get; set; }
        public Tile[] TileSet { get; set; }
        public List<Portal> Portals { get; set; } = new();
        public List<TerrainScene> TerrainScenes { get; set; } = new();
        public List<Cover> Covers { get; set; } = new();
        public List<string> Puzzles { get; set; } = new();
        public List<Effect> Effects { get; set; } = new();
        public List<Sound> Sounds { get; set; } = new();
        public List<SceneLayer> SceneLayers { get; set; } = new();

        internal static JsonDmapFile MapFrom(DmapFile dmapFile, bool ExcludeTileSet = false)
        {
            //Convert 2d tile array into single dimension
            return new JsonDmapFile()
            {
                Header = dmapFile.Header,
                MapVersion = dmapFile.MapVersion,
                PuzzleFile = dmapFile.PuzzleFile,
                SizeTiles = dmapFile.SizeTiles,
                TileSet =  ExcludeTileSet ? null : To1DTileSet(dmapFile.TileSet),
                Portals = dmapFile.Portals,
                TerrainScenes = dmapFile.TerrainScenes,
                Covers = dmapFile.Covers,
                Puzzles = dmapFile.Puzzles,
                Effects = dmapFile.Effects,
                Sounds = dmapFile.Sounds,
                SceneLayers = dmapFile.SceneLayers
            };
        }
        internal static DmapFile MapTo(JsonDmapFile jsonDmapFile)
        {
            return new DmapFile()
            {
                Header = jsonDmapFile.Header,
                MapVersion = jsonDmapFile.MapVersion,
                PuzzleFile = jsonDmapFile.PuzzleFile,
                SizeTiles = jsonDmapFile.SizeTiles,
                TileSet = To2DTileSet(jsonDmapFile.TileSet, jsonDmapFile.SizeTiles),
                Portals = jsonDmapFile.Portals,
                TerrainScenes = jsonDmapFile.TerrainScenes,
                Covers = jsonDmapFile.Covers,
                Puzzles = jsonDmapFile.Puzzles,
                Effects = jsonDmapFile.Effects,
                Sounds = jsonDmapFile.Sounds,
                SceneLayers = jsonDmapFile.SceneLayers
            };
        }

        private static Tile[] To1DTileSet(Tile[,] tileSet)
        {
            int upperbound0 = tileSet.GetUpperBound(0);
            int upperbound1 = tileSet.GetUpperBound(1);
            Tile[] tiles = new Tile[(upperbound0 + 1) * (upperbound1 + 1)];

            int index = 0;
            for (int i = 0; i <= upperbound0; i++)
            {
                for (int z = 0; z <= upperbound1; z++)
                {
                    tiles[index++] = tileSet[i, z];
                }
            }

            return tiles;
        }
        private static Tile[,] To2DTileSet(Tile[] tileSet, Size dimensions)
        {
            Tile[,] tiles = new Tile[dimensions.Width, dimensions.Height];

            int index = 0;
            for (int i = 0; i < dimensions.Width; i++)
            {
                for (int z = 0; z < dimensions.Height; z++)
                {
                    tiles[i,z] = tileSet[index++];
                }
            }

            return tiles;
        }
    }
}
