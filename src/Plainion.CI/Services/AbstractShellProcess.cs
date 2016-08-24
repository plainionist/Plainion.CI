using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Plainion.CI.Services
{
    public abstract class AbstractShellProcess : IDisposable
    {
        private Process myProcess;
        private readonly string myExecutable;
        private readonly string myWorkingDirectory;

        protected AbstractShellProcess( string executable )
            : this( executable, null )
        {
        }

        protected AbstractShellProcess( string executable, string workingDirectory )
        {
            myExecutable = executable;
            myWorkingDirectory = workingDirectory;

            Environment = new Dictionary<string, string>();
        }

        protected string Executable
        {
            get { return myExecutable; }
        }

        public virtual void Dispose()
        {
            if( myProcess == null )
            {
                return;
            }

            myProcess.CancelOutputRead();
            myProcess.CancelErrorRead();

            myProcess.OutputDataReceived -= ParseStdOut;
            myProcess.ErrorDataReceived -= ParseStdErr;

            myProcess.Kill();
            myProcess.Close();

            myProcess = null;
        }

        public IDictionary<string, string> Environment { get; private set; }

        protected void Execute( string arguments )
        {
            try
            {
                myProcess = new Process();
                myProcess.StartInfo.FileName = myExecutable;
                myProcess.StartInfo.Arguments = arguments;

                foreach( var entry in Environment )
                {
                    myProcess.StartInfo.EnvironmentVariables[ entry.Key ] = entry.Value;
                }

                if( myWorkingDirectory != null )
                {
                    myProcess.StartInfo.WorkingDirectory = myWorkingDirectory;
                }

                myProcess.StartInfo.UseShellExecute = false;
                myProcess.StartInfo.CreateNoWindow = true;
                myProcess.StartInfo.RedirectStandardOutput = true;
                myProcess.StartInfo.RedirectStandardError = true;

                myProcess.OutputDataReceived += ParseStdOut;
                myProcess.ErrorDataReceived += ParseStdErr;

                myProcess.Start();

                myProcess.BeginOutputReadLine();
                myProcess.BeginErrorReadLine();

                myProcess.WaitForExit();

                ExitCode = myProcess.ExitCode;
            }
            finally
            {
                myProcess.OutputDataReceived -= ParseStdOut;
                myProcess.ErrorDataReceived -= ParseStdErr;

                myProcess.Dispose();
                myProcess = null;
            }
        }

        private void ParseStdOut( object sender, DataReceivedEventArgs e )
        {
            OnOutputDataReceived( e.Data );
        }

        protected abstract void OnOutputDataReceived( string line );

        private void ParseStdErr( object sender, DataReceivedEventArgs e )
        {
            OnErrorDataReceived( e.Data );
        }

        protected abstract void OnErrorDataReceived( string line );

        public int ExitCode { get; private set; }
    }
}
