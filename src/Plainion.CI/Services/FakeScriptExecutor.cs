using System;
using System.IO;
using System.Linq;

namespace Plainion.CI.Services
{
    sealed class FakeScriptExecutor
    {
        private IProgress<string> myProgress;
        private BuildDefinition myBuildDefinition;

        public FakeScriptExecutor( BuildDefinition buildDefinition, IProgress<string> progress )
        {
            Contract.RequiresNotNull( buildDefinition, "buildDefinition" );
            Contract.RequiresNotNull( progress, "progress" );

            myBuildDefinition = buildDefinition;
            myProgress = progress;
        }

        public bool Execute( string script, params string[] args )
        {
            Contract.Requires( !string.IsNullOrWhiteSpace( script ), "No script given" );

            if( !Path.IsPathRooted( script ) )
            {
                script = Path.Combine( myBuildDefinition.RepositoryRoot, script );
            }

            var home = Path.GetDirectoryName( GetType().Assembly.Location );
            var interpreter = Path.Combine( home, "FAKE", "fake.exe" );

            var process = new UiShellCommand( interpreter, myProgress );

            var toolsHome = Path.GetDirectoryName( GetType().Assembly.Location );
            var PATH = Environment.GetEnvironmentVariable( "PATH" );

            // extend PATH so that FAKE targets can find the tools
            process.Environment[ "PATH" ] = Path.Combine( toolsHome, "FAKE" )
                + Path.PathSeparator + Path.Combine( toolsHome, "NuGet" )
                + Path.PathSeparator + PATH;

            process.Environment[ "ToolsHome" ] = toolsHome;
            process.Environment[ "BuildDefinitionFile" ] = BuildDefinitionSerializer.GetLocation( myBuildDefinition );

            process.Environment[ "ProjectRoot" ] = myBuildDefinition.RepositoryRoot;
            process.Environment[ "outputPath " ] = myBuildDefinition.GetOutputPath();

            var compiledArguments = new[] { "--fsiargs \"--define:FAKE\"", script }
                .Concat( args )
                .ToArray();

            process.Execute( compiledArguments );

            return process.ExitCode == 0;
        }
    }
}
