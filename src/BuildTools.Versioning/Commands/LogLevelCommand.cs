using System;

using Utility.ADL;
using Utility.CommandRunner;

namespace BuildTools.Versioning.Commands
{
    public class LogLevelCommand : AbstractCommand
    {

        public LogLevelCommand() : base(new[] { "--log-level", "-l" }, "Changes the Log Level.")
        {
            CommandAction = SetLogLevel;
        }

        private void SetLogLevel(StartupArgumentInfo arg1, string[] arg2)
        {
            if (int.TryParse(arg2[0], out int result))
            {
                CommandRunnerDebugConfig.Settings.MinSeverity = (Verbosity) result;
            }
            else if (Enum.TryParse(arg2[0], out Verbosity enumResult))
            {
                CommandRunnerDebugConfig.Settings.MinSeverity = enumResult;
            }
        }

    }
}