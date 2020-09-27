using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace BuildTools.Plugins
{
    public class PluginCreator : IBuildTool
    {

        public string Name => "plugin";

        public void Run(string[] args)
        {
            if(args.Length==1)
            {
                if (Directory.Exists(args[0]))
                {
                    string[] configFiles = Directory.GetFiles(args[0], "*.build", SearchOption.AllDirectories);
                    foreach (string configFile in configFiles)
                    {
                        Run(new []{configFile});
                    }

                    return;
                }
            }
            else
            {
                Console.WriteLine("Invalid argument.");
            }



            string configPath = args[0];

            Console.WriteLine("Running Config: " + configPath);

            string[] data = File.ReadAllLines(configPath);
            Dictionary<string, string> dataKVPs =
                data.ToDictionary(x => x.Split(':').First().Trim(), x => x.Split(':').Last().Trim());

            string rootDir = Path.GetDirectoryName(Path.GetFullPath(configPath));

            (string, string)[] includes = dataKVPs.ContainsKey("include") ? AggregateIncludes(rootDir, dataKVPs["include"].Split(';')) : new (string, string)[0];
            (string, string)[] configs = dataKVPs.ContainsKey("config") ? AggregateIncludes(rootDir, dataKVPs["config"].Split(';')) : new (string, string)[0];
            string targetFile = Path.Combine(rootDir, dataKVPs["target"]);
            string pluginName = dataKVPs["name"];
            string pluginVersion = dataKVPs.ContainsKey("version") ? dataKVPs["version"] : GetVersion(targetFile);
            string outputFile = dataKVPs.ContainsKey("output") ? Path.Combine(rootDir, dataKVPs["output"]) : Path.GetFullPath(".\\build\\" + pluginName + ".zip");
            string parentOutput = Path.GetDirectoryName(outputFile);
            string dependInfo = dataKVPs.ContainsKey("depends") ? dataKVPs["depends"] : "";
            if (!Directory.Exists(parentOutput)) Directory.CreateDirectory(parentOutput);

            string tempDir = Path.Combine(Path.GetTempPath(), pluginName + "_build");
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            Directory.CreateDirectory(tempDir);
            Directory.CreateDirectory(Path.Combine(tempDir, "bin"));
            Directory.CreateDirectory(Path.Combine(tempDir, "config"));

            Console.WriteLine($"Copying {includes.Length + configs.Length} Files");
            CopyFiles(rootDir, includes, Path.Combine(tempDir, "bin"));
            CopyFiles(rootDir, configs, Path.Combine(tempDir, "config"));

            Console.WriteLine($"Writing File Info");
            File.Copy(targetFile, Path.Combine(tempDir, "bin", Path.GetFileName(targetFile)));
            File.WriteAllText(Path.Combine(tempDir, "info.txt"), $"{pluginName}|{Path.GetFileName(targetFile)}|{pluginVersion}|{dependInfo}");

            if (File.Exists(outputFile)) File.Delete(outputFile);
            ZipFile.CreateFromDirectory(tempDir, outputFile);

            Directory.Delete(tempDir, true);
            Console.WriteLine($"Finished building.");
        }


        private string GetVersion(string file)
        {
            return FileVersionInfo.GetVersionInfo(file).ProductVersion;
        }

        private (string, string)[] AggregateIncludes(string rootDir, string[] includes)
        {
            List<(string, string)> ret = new List<(string, string)>();
            foreach (string fullInclude in includes)
            {
                if (fullInclude.Contains('*'))
                {
                    string[] parts = fullInclude.Split('*');
                    ret.AddRange(Directory.GetFiles(Path.Combine(rootDir, parts[0]), parts[1], SearchOption.AllDirectories).Select(x => (Path.Combine(rootDir, parts[0]), x)));
                }
                else if (Directory.Exists(Path.Combine(rootDir, fullInclude)))
                {
                    ret.AddRange(Directory.GetFiles(Path.Combine(rootDir, fullInclude), "*", SearchOption.AllDirectories).Select(x => (Path.Combine(rootDir, fullInclude), x)));
                }
                else
                {
                    ret.Add((Path.GetFullPath(Path.Combine(rootDir, Path.GetDirectoryName(fullInclude))), Path.GetFullPath(Path.Combine(rootDir, fullInclude))));
                }
            }

            return ret.ToArray();
        }

        private void CopyFiles(string rootDir, (string, string)[] includes, string targetDir)
        {
            foreach ((string, string) include in includes)
            {
                string target = Path.Combine(targetDir, include.Item2.Remove(0, include.Item1.Length + 1));
                string dir = Path.GetDirectoryName(target);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.Copy(include.Item2, target);
            }
        }
    }
}
