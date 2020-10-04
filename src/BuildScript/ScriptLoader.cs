using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BuildScript
{
    public static class ScriptLoader
    {

        public const string INCLUDE_FILES = "include";
        public const string CONFIG_FILES = "config";
        public const string PLUGIN_NAME = "name";
        public const string VERSION = "version";
        public const string DEPENDENCY = "dependency";
        public const string TARGET_ASSEMBLY = "target";
        public const string OUTPUT = "output";
        public const string FLAGS = "flags";
        public const string ORIGIN = "origin";
        public const string COMPILE_COMMAND = "buildcmd";
        public const string SOLUTION = "solution";

        public const char SCRIPT_LIST_SEPARATOR = ';';
        public const char SCRIPT_KEY_VALUE_SEPARATOR = ':';

        public static string[] ParseList(string data)
        {
            return data.Split(SCRIPT_LIST_SEPARATOR);
        }

        public static Dictionary<string, string> ParseScript(string[] data, bool throwOnError)
        {
            Dictionary<string, string> ret = data.Select(x => x.Split('#').First()).Where(x => !string.IsNullOrEmpty(x)).ToDictionary(
                                     x => x.Split(SCRIPT_KEY_VALUE_SEPARATOR).First().Trim(),
                                     x => x.Split(SCRIPT_KEY_VALUE_SEPARATOR).Skip(1).Unpack().Trim()
                                    );
            ResolveVariables(ret, throwOnError);
            return ret;
        }

        private static void ResolveVariables(Dictionary<string, string> data, bool throwOnError)
        {
            Dictionary<string, bool> varMap = data.Keys.Select(x => (x, data[x].Contains("%"))).ToDictionary(x => x.Item1, x => x.Item2);

            while (varMap.Values.Any(x => x))
            {
                Dictionary<string, bool> newVarMap = new Dictionary<string, bool>(varMap);
                foreach(KeyValuePair<string, bool> keyValuePair in varMap)
                {
                    if(!keyValuePair.Value)continue;


                    string key = keyValuePair.Key;
                    (int start, int end)[] vars = GetAllVariables(data[key]);

                    int resolveCount = 0;
                    for (int i = vars.Length - 1; i >= 0; i--)
                    {
                        (int start, int end) variablePosition = vars[i];
                        string varName = data[key].Substring(
                                                             variablePosition.start,
                                                             variablePosition.end - variablePosition.start + 1
                                                            ).Trim('%');
                        if (data.ContainsKey(varName))
                        {
                            if (!varMap[varName])
                            {
                                data[key] = data[key].Remove(
                                                             variablePosition.start,
                                                             variablePosition.end - variablePosition.start + 1
                                                            ).Insert(variablePosition.start, data[varName]);
                                resolveCount++;
                            }
                        }
                        else
                        {
                            Console.WriteLine("Can not Find Variable: " + varName);
                            if (throwOnError)
                                throw new InvalidOperationException();
                            else
                            {
                                return;
                            }
                        }
                    }

                    if (resolveCount == vars.Length)
                    {
                        newVarMap[key] = false;
                    }
                }

                varMap = newVarMap;

            }
        }

        private static (int start, int end)[] GetAllVariables(string line)
        {
            int idx = line.IndexOf('%');
            List<(int start, int end)> ret = new List<(int start, int end)>();
            while (idx != -1)
            {
                int next = line.IndexOf('%', idx + 1);
                if (next == -1)
                {
                    throw new Exception("Invalid Variable Syntax.");
                }
                ret.Add((idx, next));
                idx = line.IndexOf('%', next + 1);
            }

            return ret.ToArray();
        }

        private static string Unpack(this IEnumerable<string> arr)
        {
            string[] array = arr.ToArray();
            if (array.Length == 0)
            {
                return "";
            }

            StringBuilder sb = new StringBuilder(array[0]);
            for (int i = 1; i < array.Length; i++)
            {
                sb.Append(SCRIPT_KEY_VALUE_SEPARATOR + array[i]);
            }

            return sb.ToString();
        }

    }
}