using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace BuildTools.Console
{
    internal class Program
    {

        internal static void Main(string[] args)
        {
            if (args.Length != 0)
            {
                IBuildTool[] tools = GetBuildTools();
                IBuildTool selected = tools.FirstOrDefault(x => x.Name == args[0]);
                selected?.Run(args.Skip(1).ToArray());
            }
            else
            {
                System.Console.WriteLine("Argument Mismatch");
            }

#if DEBUG
            System.Console.ReadLine();
#endif
        }

        private static IBuildTool[] GetBuildTools()
        {
            List<IBuildTool> tools = new List<IBuildTool>();
            string path = Path.Combine(
                                       Path.GetDirectoryName(
                                                             new Uri(Assembly.GetExecutingAssembly().Location)
                                                                 .AbsolutePath
                                                            ),
                                       "tools"
                                      );
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string[] files = Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                try
                {
                    Assembly asm = Assembly.LoadFrom(file);
                    tools.AddRange(BuildTools.GetBuildTools(asm));
                }
                catch (Exception e)
                {
                    System.Console.WriteLine("Loading " + file + " failed.");
                }
            }

            return tools.ToArray();
        }

    }
}