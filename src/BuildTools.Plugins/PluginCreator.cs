using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;

using BuildScript;

using CommandlineSystem;

namespace BuildTools.Plugins
{
    public abstract class ZipBuildCreator : ICommandlineSystem
    {

        public abstract string Name { get; }
        public abstract string Extension { get; }

        public void Run(string[] args)
        {
            if (args.Length == 1)
            {
                if (Directory.Exists(args[0]))
                {
                    string[] configFiles = Directory.GetFiles(
                                                              Path.GetFullPath(args[0]),
                                                              $"*.{Extension}",
                                                              SearchOption.AllDirectories
                                                             );
                    foreach (string configFile in configFiles)
                    {
                        try
                        {
                            Run(new[] { configFile });
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"[{configFile}] Build Failure: {e.Message}");
                        }
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
            Dictionary<string, string> dataKVPs = ScriptLoader.ParseScript(data);


            string rootDir = Path.GetDirectoryName(Path.GetFullPath(configPath));

            (string, string)[] includes = dataKVPs.ContainsKey(ScriptLoader.INCLUDE_FILES)
                                              ? AggregateIncludes(
                                                                  rootDir,
                                                                  ScriptLoader.ParseList(
                                                                                         dataKVPs[ScriptLoader
                                                                                                      .INCLUDE_FILES]
                                                                                        )
                                                                 )
                                              : new (string, string)[0];
            (string, string)[] configs = dataKVPs.ContainsKey(ScriptLoader.CONFIG_FILES)
                                             ? AggregateIncludes(
                                                                 rootDir,
                                                                 ScriptLoader.ParseList(
                                                                                        dataKVPs[ScriptLoader
                                                                                                     .CONFIG_FILES]
                                                                                       )
                                                                )
                                             : new (string, string)[0];
            string targetFile = Path.Combine(rootDir, dataKVPs[ScriptLoader.TARGET_ASSEMBLY]);
            string pluginName = dataKVPs[ScriptLoader.PLUGIN_NAME];
            string pluginVersion = dataKVPs.ContainsKey(ScriptLoader.VERSION)
                                       ? dataKVPs[ScriptLoader.VERSION]
                                       : GetVersion(targetFile);
            string outputFile = dataKVPs.ContainsKey(ScriptLoader.OUTPUT)
                                    ? Path.Combine(rootDir, dataKVPs[ScriptLoader.OUTPUT])
                                    : Path.GetFullPath(".\\build\\" + pluginName + ".zip");
            string parentOutput = Path.GetDirectoryName(outputFile);
            string dependInfo = dataKVPs.ContainsKey(ScriptLoader.DEPENDENCY) ? dataKVPs[ScriptLoader.DEPENDENCY] : "";
            string origin = dataKVPs.ContainsKey(ScriptLoader.ORIGIN) ? dataKVPs[ScriptLoader.ORIGIN] : "";
            string[] flags = dataKVPs.ContainsKey(ScriptLoader.FLAGS)
                                 ? ScriptLoader.ParseList(dataKVPs[ScriptLoader.FLAGS])
                                 : new string[0];
            if (!Directory.Exists(parentOutput))
            {
                Directory.CreateDirectory(parentOutput);
            }

            string tempDir = Path.Combine(Path.GetTempPath(), pluginName + "_build");
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }

            Directory.CreateDirectory(tempDir);
            string binDir;
            string configDir;
            if (flags.Contains("NO_STRUCTURE"))
            {
                binDir = tempDir;
                configDir = tempDir;
            }
            else
            {
                binDir = Path.Combine(tempDir, "bin");
                configDir = Path.Combine(tempDir, "config");
            }
            Directory.CreateDirectory(binDir);
            Directory.CreateDirectory(configDir);

            Console.WriteLine($"Copying {includes.Length + configs.Length} Files");
            CopyFiles(includes, binDir);
            CopyFiles(configs, configDir);

            Console.WriteLine("Writing File Info");
            File.Copy(targetFile, Path.Combine(binDir, Path.GetFileName(targetFile)));
            string fileContent = $"{pluginName}|{Path.GetFileName(targetFile)}|{origin}|{pluginVersion}|{dependInfo}";
            if (!flags.Contains("NO_INFO_TO_ZIP"))
            {
                File.WriteAllText(Path.Combine(tempDir, "info.txt"), fileContent);
            }

            if (flags.Contains("INFO_TO_OUTPUT"))
            {
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(outputFile), "info.txt"), fileContent);
            }

            if (File.Exists(outputFile))
            {
                File.Delete(outputFile);
            }

            ZipFile.CreateFromDirectory(tempDir, outputFile);

            Directory.Delete(tempDir, true);
            Console.WriteLine("Finished building.");
        }


        private string GetVersion(string file)
        {
            return FileVersionInfo.GetVersionInfo(file).FileVersion;
        }

        private (string, string)[] AggregateIncludes(string rootDir, string[] includes)
        {
            List<(string, string)> ret = new List<(string, string)>();
            foreach (string fullInclude in includes)
            {
                if (fullInclude.Contains('*'))
                {
                    string[] parts = fullInclude.Replace("/", "\\").Split('*');
                    int findLast = parts[0].LastIndexOf("\\");
                    string pre = parts[0].Remove(0, findLast + 1);
                    string dir = parts[0].Remove(findLast, parts[0].Length - findLast);
                    ret.AddRange(
                                 Directory.GetFiles(
                                                    Path.Combine(rootDir, dir),
                                                    pre + "*" + parts[1],
                                                    SearchOption.AllDirectories
                                                   ).Select(x => (Path.Combine(rootDir, dir), x))
                                );
                }
                else if (Directory.Exists(Path.Combine(rootDir, fullInclude)))
                {
                    ret.AddRange(
                                 Directory.GetFiles(
                                                    Path.Combine(rootDir, fullInclude),
                                                    "*",
                                                    SearchOption.AllDirectories
                                                   ).Select(x => (Path.Combine(rootDir, fullInclude), x))
                                );
                }
                else
                {
                    ret.Add(
                            (Path.GetFullPath(Path.Combine(rootDir, Path.GetDirectoryName(fullInclude))),
                             Path.GetFullPath(Path.Combine(rootDir, fullInclude)))
                           );
                }
            }

            return ret.ToArray();
        }

        private void CopyFiles((string, string)[] includes, string targetDir)
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

    public class ApplicationCreator : ZipBuildCreator
    {

        public override string Name => "app";

        public override string Extension => "app";

    }

    public class PluginCreator : ZipBuildCreator
    {

        public override string Name => "plugin";

        public override string Extension => "build";

    }
}