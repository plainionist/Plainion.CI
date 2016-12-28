using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Plainion.CI.Services
{
    sealed class FakeScriptExecutor 
    {
        public FakeScriptExecutor( BuildDefinition buildDefinition, IProgress<string> progress )
        {
            Contract.RequiresNotNull( buildDefinition, "buildDefinition" );
            Contract.RequiresNotNull( progress, "progress" );

            BuildDefinition = buildDefinition;
            Progress = progress;
        }

        private IProgress<string> Progress { get; set; }

        private BuildDefinition BuildDefinition { get; set; }

        private string Interpreter
        {
            get
            {
                var home = Path.GetDirectoryName( GetType().Assembly.Location );
                return Path.Combine( home, "FAKE", "fake.exe" );
            }
        }

        private IEnumerable<string> ValidScriptExtensions
        {
            get { yield return ".fsx"; }
        }

        public bool CanExecute( string script )
        {
            var fileExtension = Path.GetExtension( script );
            return ValidScriptExtensions.Any( x => x.Equals( fileExtension, StringComparison.OrdinalIgnoreCase ) );
        }

        public bool Execute( string script, params string[] args )
        {
            Contract.Requires( !string.IsNullOrWhiteSpace( script ), "No script given" );

            Contract.Requires( CanExecute( script ), "{0} is not a valid script. Valid file extensions are: {1}", script, string.Join( ",", ValidScriptExtensions ) );

            if( !Path.IsPathRooted( script ) )
            {
                script = Path.Combine( BuildDefinition.RepositoryRoot, script );
            }

            var process = new UiShellCommand( Interpreter, Progress );

            var toolsHome = Path.GetDirectoryName( GetType().Assembly.Location );
            var PATH = Environment.GetEnvironmentVariable( "PATH" );

            // extend PATH so that FAKE targets can find the tools
            process.Environment[ "PATH" ] = Path.Combine( toolsHome, "FAKE" )
                + Path.PathSeparator + Path.Combine( toolsHome, "NuGet" )
                + Path.PathSeparator + PATH;

            process.Environment[ "ToolsHome" ] = toolsHome;
            process.Environment[ "BuildDefinitionFile" ] = BuildDefinitionSerializer.GetLocation( BuildDefinition );

            process.Environment[ "ProjectRoot" ] = BuildDefinition.RepositoryRoot;
            process.Environment[ "outputPath " ] = BuildDefinition.GetOutputPath();

            var compiledArguments = CompileScriptArgumentsInternal( script, args ).ToArray();

            process.Execute( compiledArguments );

            return process.ExitCode == 0;
        }

        private IEnumerable<string> CompileScriptArgumentsInternal( string script, string[] args )
        {
            yield return "--fsiargs \"--define:FAKE\"";

            yield return script;

            foreach( var arg in args )
            {
                yield return arg;
            }
        }
    }
}
