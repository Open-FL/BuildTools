using System;
using System.Globalization;
using System.IO;
using System.Xml;

using BuildTools.Versioning.Commands;

using Utility.ADL;
using Utility.CommandRunner;
using Utility.CommandRunner.BuiltInCommands;

namespace BuildTools.Versioning
{
    public class VersionHelperConsole : IBuildTool
    {

        public string Name => "version";

        public void Run(string[] args)
        {
            Debug.DefaultInitialization();
            Runner.AddCommand(new LogLevelCommand());
            Runner.AddCommand(new DefaultHelpCommand());
            Runner.AddCommand(new NoWrapFlagCommand());
            Runner.AddCommand(new ToFileCommand());
            Runner.AddCommand(new GetVersionCommand());
            Runner.AddCommand(new ChangeVersionCommand());
            Runner.RunCommands(args);
        }

        private static bool IsNetFramework(XmlDocument doc)
        {
            XmlNode s = null;

            for (int i = 0; i < doc.ChildNodes.Count; i++)
            {
                if (doc.ChildNodes[i].Name == "Project")
                {
                    s = doc.ChildNodes[i];
                }
            }

            if (s != null)
            {
                for (int i = 0; i < s.ChildNodes.Count; i++)
                {
                    if (s.ChildNodes[i].Name == "PropertyGroup")
                    {
                        if (s.ChildNodes[i].HasChildNodes)
                        {
                            for (int j = 0; j < s.ChildNodes[i].ChildNodes.Count; j++)
                            {
                                XmlNode projTag = s.ChildNodes[i].ChildNodes[j];
                                if (projTag.Name == "TargetFrameworkVersion")
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        public static void ChangeVersionInFile(string file, Version newVersion)
        {
            if (file.EndsWith("ProjectSettings.asset"))
            {
                string source = File.ReadAllText(file);
                int start = source.IndexOf("bundleVersion: ") + "bundleVersion: ".Length;
                int end = source.IndexOf('\n', start);

                source = source.Remove(start, end - start);

                source = source.Insert(start, newVersion.ToString());

                File.WriteAllText(file, source);

                return;
            }

            XmlDocument doc = new XmlDocument();
            doc.Load(file);


            if (IsNetFramework(doc))
            {
                string dir = Path.GetDirectoryName(Path.GetFullPath(file));
                string asmFile = Path.Combine(dir, "Properties", "AssemblyInfo.cs");

                string[] src = File.ReadAllLines(asmFile);

                for (int i = 0; i < src.Length; i++)
                {
                    if (src[i].Trim().StartsWith("[assembly: AssemblyVersion(\""))
                    {
                        src[i] = $"[assembly: AssemblyVersion(\"{newVersion}\")]";
                    }
                    else if (src[i].Trim().StartsWith("[assembly: AssemblyFileVersion(\""))
                    {
                        src[i] = $"[assembly: AssemblyFileVersion(\"{newVersion}\")]";
                    }
                }

                File.Delete(asmFile);
                File.WriteAllLines(asmFile, src);

                return;
            }

            XmlNode[] nodes = FindVersionTags(doc);

            nodes[0].InnerText = newVersion.ToString();
            nodes[1].InnerText = newVersion.ToString();
            try
            {
                doc.Save(file);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public static Version GetVersionFromFile(string file)
        {
            if (file.EndsWith("ProjectSettings.asset"))
            {
                string source = File.ReadAllText(file);
                int start = source.IndexOf("bundleVersion: ") + "bundleVersion: ".Length;
                int end = source.IndexOf('\n', start);
                string v = source.Substring(start, end - start);
                return Version.Parse(v);
            }
            XmlDocument doc = new XmlDocument();
            doc.Load(file);
            if (IsNetFramework(doc))
            {
                string dir = Path.GetDirectoryName(Path.GetFullPath(file));
                string asmFile = Path.Combine(dir, "Properties", "AssemblyInfo.cs");

                string[] src = File.ReadAllLines(asmFile);

                for (int i = 0; i < src.Length; i++)
                {
                    if (src[i].Trim().StartsWith("[assembly: AssemblyVersion(\""))
                    {
                        string[] v = src[i].Trim().Split('\"');
                        return Version.Parse(v[1]);
                    }
                }

                return new Version(0, 0, 1, 0);
            }

            return FindVersion(doc);
        }

        public static Version ChangeVersion(Version version, string changeStr)
        {
            string[] subVersions = changeStr.Split('.');
            int[] wrapValues = { ushort.MaxValue, 9, 99, ushort.MaxValue };
            int[] versions = { version.Major, version.Minor, version.Build, version.Revision };
            for (int i = 4 - 1; i >= 0; i--)
            {
                string current = subVersions[i];
                if (current.StartsWith("("))
                {
                    if (i == 0)
                    {
                        continue; //Can not wrap the last digit
                    }

                    int j = 0;
                    for (; j < current.Length; j++)
                    {
                        if (current[j] == ')')
                        {
                            break;
                        }
                    }

                    if (j == current.Length)
                    {
                        Console.WriteLine($"Can not parse version ID: {i}({current})");
                        continue; //Broken. No number left. better ignore
                    }

                    string max = current.Substring(1, j - 1);
                    if (int.TryParse(max, out int newMax))
                    {
                        wrapValues[i] = newMax;
                    }

                    current = current.Remove(0, j + 1);
                }

                if (!NoWrapFlagCommand.NoWrap && i != 0) //Check if we wrapped
                {
                    if (versions[i] >= wrapValues[i])
                    {
                        versions[i] = 0;
                        versions[i - 1]++;
                    }
                }

                if (current == "+")
                {
                    versions[i]++;
                }
                else if (current == "-" && versions[i] != 0)
                {
                    versions[i]--;
                }
                else if (current.ToLower(CultureInfo.InvariantCulture) == "x")
                {
                }
                else if (current.StartsWith("{") && current.EndsWith("}"))
                {
                    string format = current.Remove(current.Length - 1, 1).Remove(0, 1);

                    string value = DateTime.Now.ToString(format);

                    if (long.TryParse(value, out long newValue))
                    {
                        versions[i] = (int)(newValue % ushort.MaxValue);
                    }
                    else
                    {
                        Console.WriteLine("Can not Parse: " + value + " to INT");
                    }
                }
                else if (int.TryParse(current, out int v))
                {
                    versions[i] = v;
                }
            }

            return new Version(versions[0], versions[1] < 0 ? 0 : versions[1], versions[2] < 0 ? 0 : versions[2], versions[3] < 0 ? 0 : versions[3]);
        }

        private static Version FindVersion(XmlDocument doc)
        {
            if (Version.TryParse(FindVersionTags(doc)[0].InnerText, out Version v))
            {
                return v;
            }

            return new Version(0, 0, 1, 0);
        }

        private static XmlNode[] FindVersionTags(XmlDocument doc)
        {
            XmlNode s = null;

            for (int i = 0; i < doc.ChildNodes.Count; i++)
            {
                if (doc.ChildNodes[i].Name == "Project")
                {
                    s = doc.ChildNodes[i];
                }
            }

            XmlNode[] ret = new XmlNode[2];
            if (s != null)
            {
                for (int i = 0; i < s.ChildNodes.Count; i++)
                {
                    if (s.ChildNodes[i].Name == "PropertyGroup")
                    {
                        if (s.ChildNodes[i].HasChildNodes && s.ChildNodes[i].FirstChild.Name == "TargetFramework")
                        {
                            for (int j = 0; j < s.ChildNodes[i].ChildNodes.Count; j++)
                            {
                                XmlNode projTag = s.ChildNodes[i].ChildNodes[j];
                                if (projTag.Name == "AssemblyVersion")
                                {
                                    ret[0] = projTag;
                                }
                                else if (projTag.Name == "FileVersion")
                                {
                                    ret[1] = projTag;
                                }
                            }
                        }
                    }
                }
            }

            if (ret[0] == null || ret[1] == null)
            {
                for (int i = 0; i < s.ChildNodes.Count; i++)
                {
                    if (s.ChildNodes[i].Name == "PropertyGroup")
                    {
                        if (s.ChildNodes[i].HasChildNodes && s.ChildNodes[i].FirstChild.Name == "TargetFramework")
                        {
                            XmlNode assemblyVersion = s.ChildNodes[i]
                                .AppendChild(doc.CreateNode(XmlNodeType.Element, "AssemblyVersion", ""));
                            assemblyVersion.InnerText = "0.0.1.0";
                            XmlNode fileVersion = s.ChildNodes[i]
                                .AppendChild(doc.CreateNode(XmlNodeType.Element, "FileVersion", ""));
                            fileVersion.InnerText = "0.0.1.0";
                            ret[0] = assemblyVersion;
                            ret[1] = fileVersion;
                        }
                    }
                }
            }

            return ret;
        }
    }
}