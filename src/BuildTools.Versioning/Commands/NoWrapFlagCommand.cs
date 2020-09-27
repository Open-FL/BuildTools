
using Utility.CommandRunner;

namespace BuildTools.Versioning.Commands
{
    public class NoWrapFlagCommand : AbstractCommand
    {
        public static bool NoWrap;

        public NoWrapFlagCommand() : base(new[] {"--no-wrap", "-nw"}, "Disables the Max Values for the Version Strings")
        {
            CommandAction = (info, strings) => NoWrapFlag();
        }

        private void NoWrapFlag()
        {
            NoWrap = true;
        }
    }
}