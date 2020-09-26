using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildTools.Console
{
    internal class Program
    {
       internal static void Main(string[] args)
        {

            if (args.Length == 2)
            {
                IBuildTool[] tools = BuildTools.GetBuildTools();
                IBuildTool selected = tools.FirstOrDefault(x => x.Name == args[0]);
                selected?.Run(args[1]);
            }
            else
            {
                System.Console.WriteLine("Argument Mismatch");
            }

            System.Console.ReadLine();
        }
    }
}
