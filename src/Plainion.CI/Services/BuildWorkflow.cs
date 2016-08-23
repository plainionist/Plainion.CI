using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Plainion.CI.Model;
using Plainion.CI.Services.SourceControl;

namespace Plainion.CI.Services
{
    internal class BuildWorkflow
    {
        private ISourceControl mySourceControl;
        private BuildDefinition myDefinition;
        private BuildRequest myRequest;

        public BuildWorkflow( ISourceControl sourceControl, BuildDefinition definition, BuildRequest request )
        {
            mySourceControl = sourceControl;
            myDefinition = definition;
            myRequest = request;
        }

        internal Task<bool> ExecuteAsync( IProgress<string> progress )
        {
            // clone thread save copy of the relevant paramters;
            myDefinition = Objects.Clone( myDefinition );
            myRequest = Objects.Clone( myRequest );

            var solution = GetSolutionFile();
            var builtInMsBuildScript = Path.Combine( Path.GetDirectoryName( GetType().Assembly.Location ), "Services", "Msbuild", "Plainion.CI.targets" );
            var commonFsx = Path.Combine( Path.GetDirectoryName( GetType().Assembly.Location ), "bits", "Common.fsx" );

            return Task<bool>.Run( () =>
                Execute( "Clean", ExecuteFakeScript( commonFsx, "Clean" ), progress )
                && Execute( "update nuget packages", ExecuteFakeScript( commonFsx, "RestoreNugetPackages" ), progress )
                && Execute( "build", ExecuteMsbuildScript( solution ), progress )
                && ( !myDefinition.RunTests || RunTests( commonFsx, progress ) )
                && ( !myDefinition.CheckIn || Execute( "checkin", CheckIn, progress ) )
                && ( !myDefinition.Push || Execute( "push", Push, progress ) )
                && ( !myDefinition.CreatePackage || Execute( "create pacakge", ExecuteMsbuildScript( myDefinition.CreatePackageScript,
                    myDefinition.CreatePackageArguments.Split( new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries ) ), progress ) )
                && ( !myDefinition.DeployPackage || Execute( "deploy pacakge", ExecuteMsbuildScript( myDefinition.DeployPackageScript,
                    myDefinition.DeployPackageArguments.Split( new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries ) ), progress ) )
            );
        }

        private string GetSolutionFile()
        {
            return Path.Combine( myDefinition.RepositoryRoot, myDefinition.Solution );
        }

        private bool Execute( string activity, Func<IProgress<string>, bool> action, IProgress<string> progress )
        {
            try
            {
                var success = action( progress );

                if( success )
                {
                    progress.Report( "--- " + activity.ToUpper() + " SUCCEEDED ---" );
                }
                else
                {
                    progress.Report( "--- " + activity.ToUpper() + " FAILED ---" );
                }

                return success;
            }
            catch( Exception ex )
            {
                progress.Report( "ERROR: " + ex.Message );
                progress.Report( "--- " + activity.ToUpper() + " FAILED ---" );
                return false;
            }
        }

        private Func<IProgress<string>, bool> ExecuteMsbuildScript( string script, params string[] args )
        {
            Contract.Requires( !string.IsNullOrWhiteSpace( script ), "No MsBuild script given" );

            var fileExtension = Path.GetExtension( script );
            var validExtensions = new[] { ".msbuild", ".targets", ".sln" };
            Contract.Requires( validExtensions.Any( x => x.Equals( fileExtension, StringComparison.OrdinalIgnoreCase ) )
                , "{0} is not a valid MsBuild script. Valid file extensions are: {1}", script, string.Join( ",", validExtensions ) );

            return p =>
            {
                if( !Path.IsPathRooted( script ) )
                {
                    script = Path.Combine( myDefinition.RepositoryRoot, script );
                }

                var process = new UiShellCommand( @"C:\Program Files (x86)\MSBuild\12.0\Bin\MSBuild.exe", p );

                var allArgs = new[]{
                        "/m",
                        "/p:Configuration=" + myDefinition.Configuration,
                        "/p:Platform=\"" + myDefinition.Platform + "\"",
                        "/p:OutputPath=\"" + GetOutputPath() + "\"",
                        "/p:ProjectRoot=\"" + myDefinition.RepositoryRoot + "\""
                    }
                .Concat( args )
                .Concat( new[] { script } )
                .ToArray();

                process.Execute( allArgs );

                return process.ExitCode == 0;
            };
        }

        private Func<IProgress<string>, bool> ExecuteFakeScript( string script, string target, params string[] args )
        {
            Contract.Requires( !string.IsNullOrWhiteSpace( script ), "No FAKE script given" );

            var fileExtension = Path.GetExtension( script );
            var validExtensions = new[] { ".fsx" };
            Contract.Requires( validExtensions.Any( x => x.Equals( fileExtension, StringComparison.OrdinalIgnoreCase ) )
                , "{0} is not a valid FAKE script. Valid file extensions are: {1}", script, string.Join( ",", validExtensions ) );

            return p =>
            {
                if( !Path.IsPathRooted( script ) )
                {
                    script = Path.Combine( myDefinition.RepositoryRoot, script );
                }

                var home = Path.GetDirectoryName( GetType().Assembly.Location );
                var process = new UiShellCommand( Path.Combine( home, "FAKE", "fake.exe" ), p );

                var allArgs = new[] { script, target }
                    .Concat( new[]{
                        "OutputPath=\"" + GetOutputPath() + "\"",
                        "ProjectRoot=\"" + myDefinition.RepositoryRoot + "\"",
                        "SolutionFile=\"" + GetSolutionFile() + "\"",
                        "--printdetails"
                    } )
                    .Concat( args )
                    .ToArray();

                process.Execute( allArgs );

                return process.ExitCode == 0;
            };
        }

        private string GetOutputPath()
        {
            return Path.Combine( myDefinition.RepositoryRoot, "bin", "gc" );
        }

        private bool RunTests( string builtInScript, IProgress<string> progress )
        {
            return Execute( "test", ExecuteFakeScript( builtInScript,
                "RunNUnitTests",
                "NUnitPath=" + Path.GetDirectoryName( myDefinition.TestRunnerExecutable ),
                "TestAssemblyPattern=" + myDefinition.TestAssemblyPattern
                ), progress );
        }

        private bool CheckIn( IProgress<string> progress )
        {
            if( string.IsNullOrEmpty( myRequest.CheckInComment ) )
            {
                progress.Report( "!! NO CHECKIN COMMENT PROVIDED !!" );
                return false;
            }

            mySourceControl.Commit( myDefinition.RepositoryRoot, myRequest.Files, myRequest.CheckInComment, myDefinition.User.Login, myDefinition.User.EMail );

            return true;
        }

        private bool Push( IProgress<string> progress )
        {
            if( myDefinition.User.Password == null )
            {
                progress.Report( "!! NO PASSWORD PROVIDED !!" );
                return false;
            }

            mySourceControl.Push( myDefinition.RepositoryRoot, myDefinition.User.Login, myDefinition.User.Password.ToUnsecureString() );

            return true;
        }
    }
}
