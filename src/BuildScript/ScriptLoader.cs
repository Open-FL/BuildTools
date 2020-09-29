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

        public const char SCRIPT_LIST_SEPARATOR = ';';
        public const char SCRIPT_KEY_VALUE_SEPARATOR = ':';

        public static string[] ParseList(string data)
        {
            return data.Split(SCRIPT_LIST_SEPARATOR);
        }

        public static Dictionary<string, string> ParseScript(string[] data)
        {
            return data.ToDictionary(
                                     x => x.Split(SCRIPT_KEY_VALUE_SEPARATOR).First().Trim(),
                                     x => x.Split(SCRIPT_KEY_VALUE_SEPARATOR).Skip(1).Unpack().Trim()
                                    );
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