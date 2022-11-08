using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Tiled2Dmap.CLI.Dmap
{
    public class Ani
    {
        public string Name { get; set; }
        public Queue<string> Frames { get; set; } = new();
    }
    public class AniFile
    {
        public Dictionary<string, Ani> Anis { get; set; } = new();
        public string AniFilePath { get; set; }

        private string ClientPath;

        public AniFile(string AniFilePath)
        {
            this.AniFilePath = AniFilePath;
        }
        public AniFile(string ClientPath, string AniFilePath)
        {
            this.ClientPath = ClientPath;
            this.AniFilePath = AniFilePath;
            this.Load();
        }

        public void Save(string ProjectDirectory)
        {
            string newPath = Path.Combine(ProjectDirectory, AniFilePath);

            string? outDir = Path.GetDirectoryName(newPath);
            if (outDir == null) { Log.Warn($"Unable to copy resources to {newPath}, can't retrieve directory"); return; }

            if (!Directory.Exists(outDir))
                Directory.CreateDirectory(outDir);



            using(TextWriter tw = new StreamWriter(File.OpenWrite(newPath)))
            {
                foreach(var ani in this.Anis.Values)
                {
                    tw.WriteLine($"[{ani.Name}]");
                    tw.WriteLine($"FrameAmount={ani.Frames.Count}");
                    int frameCount = 0;
                    foreach(string frame in ani.Frames)
                    {
                        tw.WriteLine($"Frame{frameCount++}={frame}");
                    }
                    tw.WriteLine();
                }
            }
        }

        public void Load()
        {
            if (Path.IsPathFullyQualified(this.AniFilePath))
                this.AniFilePath = Path.GetRelativePath(ClientPath, this.AniFilePath);
            
            string aniPath = Path.Combine(this.ClientPath, this.AniFilePath);

            if (!File.Exists(aniPath)) throw new FileNotFoundException($"Ani File not found at {aniPath}");

            using (TextReader tr = new StreamReader(File.OpenRead(aniPath)))
            {
                while (tr.Peek() != -1)
                {
                    string line = tr.ReadLine();
                    if (line.StartsWith("["))
                    {
                        Ani ani = new Ani();
                        ani.Name = line.Trim('[').Trim(']');

                        //Read the number of frames.
                        int frameAmount = int.Parse(new Regex(@"\d+").Match(tr.ReadLine()).Value);

                        //Read each from of the ani.
                        for (int i = 0; i < frameAmount; i++)
                        {
                            string frameLine = tr.ReadLine();
                            string ddsPath = frameLine.Split('=')[1];
                            ani.Frames.Enqueue(ddsPath);
                        }
                        this.Anis.TryAdd(ani.Name,ani);
                    }
                }
                //Console.WriteLine($"Finished reading {aniPath} ");
            }
        }

    }
}
