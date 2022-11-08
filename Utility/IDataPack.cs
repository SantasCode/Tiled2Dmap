using System.IO;

namespace Tiled2Dmap.CLI.Utility
{
    internal interface IDataPack: System.IDisposable
    {
        MemoryStream GetFileStream(string File);
    }
}
