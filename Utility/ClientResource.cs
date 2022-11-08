using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tiled2Dmap.CLI.Utility
{
    public class ClientResources : IDisposable
    {
        public string ClientDirectory { get; init; }

        private IDataPack _dataPack;

        public ClientResources(string ClientDirectory)
        {
            this.ClientDirectory = ClientDirectory;

            if (File.Exists(Path.Combine(ClientDirectory, "data.wdf")))
                _dataPack = new WindsoulDataFile(Path.Combine(ClientDirectory, "data.wdf"));
            else if (File.Exists(Path.Combine(ClientDirectory, "data.Dnp")))
                _dataPack = new DawnPack(Path.Combine(ClientDirectory, "data.Dnp"));

        }

        public Stream GetFile(string RelativePath)
        {
            if (File.Exists(Path.Combine(ClientDirectory, RelativePath.TrimEnd('/'))))
                return File.OpenRead(Path.Combine(ClientDirectory, RelativePath));

            if (_dataPack == null) return null;
            var fileStream = _dataPack.GetFileStream(RelativePath);
                       
            return fileStream;
            

        }
        public bool Copy(string DestinationDirectory, string ResourceFile)
        {
            try
            {
                using Stream fileStream = GetFile(ResourceFile);
                if (fileStream == null) return false;
                if (!Directory.Exists(DestinationDirectory)) Directory.CreateDirectory(DestinationDirectory);

                //Maintain the nested directory of the client resource.
                string completePath = Path.Combine(DestinationDirectory, ResourceFile.TrimStart('/'));

                if (!Directory.Exists(Path.GetDirectoryName(completePath))) Directory.CreateDirectory(Path.GetDirectoryName(completePath));

                using (FileStream writer = File.OpenWrite(completePath))
                {
                    fileStream.Seek(0, SeekOrigin.Begin);
                    fileStream.CopyTo(writer);
                }
                return true;
            }
            catch(FileNotFoundException fnfe)
            {
                var conColor = Console.BackgroundColor;
                Console.BackgroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Failed to find and copy {ResourceFile}");
                Console.BackgroundColor = conColor;
                return false;
            }
        }

        public void Dispose()
        {
            _dataPack?.Dispose();
        }
    }
}
