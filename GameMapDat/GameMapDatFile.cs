using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tiled2Dmap.CLI.GameMapDat
{
    internal class GameMapDatFile
    {
        private class Map
        {
            public uint MapID;
            public string DMapPath;
            public uint PuzzleSize;
            public Map(uint ID, string path, uint puzzleSize)
            {
                MapID = ID;
                DMapPath = path;
                PuzzleSize = puzzleSize;
            }
        }

        private Dictionary<uint, Map> _mapCollection = new();

        private string _path;
        internal GameMapDatFile(string Path)
        {
            _path = Path;
            Read(_path);
        }

        internal void Write(string? Path = null)
        {
            Path = Path ?? _path;
            using (BinaryWriter writer = new BinaryWriter(File.OpenWrite(Path)))
            {
                writer.Write(_mapCollection.Count);
                foreach (var map in _mapCollection.Values)
                {
                    writer.Write(map.MapID);
                    writer.Write(map.DMapPath.Length);
                    writer.Write(ASCIIEncoding.ASCII.GetBytes(map.DMapPath));
                    writer.Write(map.PuzzleSize);
                }
            }
        }
        private void Read(string path)
        {
            if (File.Exists(path))
            {
                using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))
                {
                    uint mapCount = reader.ReadUInt32();
                    List<string> uniqueMaps = new List<string>();
                    for (int idx = 0; idx < mapCount; idx++)
                    {
                        uint mapID = reader.ReadUInt32();
                        string filePath = ASCIIEncoding.ASCII.GetString(reader.ReadBytes(reader.ReadInt32()));
                        uint puzzleSize = reader.ReadUInt32();
                        if (!_mapCollection.ContainsKey(mapID))
                            _mapCollection.Add(mapID, new Map(mapID, filePath, puzzleSize));
                        if (!uniqueMaps.Contains(filePath))
                            uniqueMaps.Add(filePath);
                        //Console.WriteLine($"{mapID} -> {filePath}:{puzzleSize}");
                    }
                    //Console.WriteLine("Count: {0}", mapCount);
                    //Console.WriteLine("Unique Count: {0}", uniqueMaps.Count);
                    
                }
            }
        }
        internal bool TryAdd(uint mapId, string path, uint size, bool replace = false)
        {
            if (replace)
                _mapCollection.Remove(mapId);

            if (_mapCollection.ContainsKey(mapId))
            {
                Log.Error("MapId already taken");
                return false;
            }

            Map newMap = new(mapId, path, size);
            return _mapCollection.TryAdd(mapId, newMap);
        }
    }
}
