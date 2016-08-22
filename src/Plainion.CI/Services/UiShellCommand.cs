using System;

namespace Plainion.CI.Services
{
    public class UiShellCommand : AbstractShellProcess
    {
        private IProgress<string> myProgress;

        public UiShellCommand(string executable, IProgress<string> progress)
            : base(executable)
        {
            myProgress = progress;
        }

        public UiShellCommand(string executable, string workingDirectory, IProgress<string> progress)
            : base(executable, workingDirectory)
        {
            myProgress = progress;
        }

        public void Execute(params string[] arguments)
        {
            base.Execute(string.Join(" ", arguments));
        }

        protected override void OnOutputDataReceived(string line)
        {
            myProgress.Report(line);
        }

        protected override void OnErrorDataReceived(string line)
        {
            myProgress.Report(line);
        }
    }
}
