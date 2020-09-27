using System;
using System.Linq;
using System.Reflection;

namespace BuildTools
{
    public static class BuildTools
    {

        public static IBuildTool[] GetBuildTools(Assembly target)
        {
            Type[] asmTypes = target.GetTypes().Where(x => typeof(IBuildTool).IsAssignableFrom(x) && !x.IsAbstract && !x.IsInterface).ToArray();
            IBuildTool[] ret = new IBuildTool[asmTypes.Length];
            for (int i = 0; i < asmTypes.Length; i++)
            {
                Type asmType = asmTypes[i];
                ret[i] = (IBuildTool)Activator.CreateInstance(asmType);
            }

            return ret;
        }

    }
}