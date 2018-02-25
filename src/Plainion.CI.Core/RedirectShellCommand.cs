using System;
using System.IO;

namespace Plainion.CI
{
    public class RedirectShellCommand : AbstractShellProcess
    {
        public RedirectShellCommand(string executable)
            : base(executable)
        {
        }

        public RedirectShellCommand(string executable, string workingDirectory)
            : base(executable, workingDirectory)
        {
        }

        public TextWriter Out { get; set; } = Console.Out;
        public TextWriter Error { get; set; } = Console.Error;

        protected override void OnOutputDataReceived(string line)
        {
            Out?.WriteLine(line);
        }

        protected override void OnErrorDataReceived(string line)
        {
            Error?.WriteLine(line);
        }
    }
}
