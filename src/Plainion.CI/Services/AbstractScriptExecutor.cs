﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Plainion.CI.Model;

namespace Plainion.CI.Services
{
    abstract class AbstractScriptExecutor
    {
        protected AbstractScriptExecutor( BuildDefinition buildDefinition, IProgress<string> progress )
        {
            Contract.RequiresNotNull( buildDefinition, "buildDefinition" );
            Contract.RequiresNotNull( progress, "progress" );

            BuildDefinition = buildDefinition;
            Progress = progress;
        }

        protected IProgress<string> Progress { get; private set; }

        protected BuildDefinition BuildDefinition { get; private set; }

        protected abstract string Interpreter { get; }

        protected abstract IEnumerable<string> ValidScriptExtensions { get; }

        public bool CanExecute( string script )
        {
            var fileExtension = Path.GetExtension( script );
            return ValidScriptExtensions.Any( x => x.Equals( fileExtension, StringComparison.OrdinalIgnoreCase ) );
        }

        public bool Execute( string script, string target, params string[] args )
        {
            Contract.Requires( !string.IsNullOrWhiteSpace( script ), "No script given" );

            Contract.Requires( CanExecute( script ), "{0} is not a valid script. Valid file extensions are: {1}", script, string.Join( ",", ValidScriptExtensions ) );

            if( !Path.IsPathRooted( script ) )
            {
                script = Path.Combine( BuildDefinition.RepositoryRoot, script );
            }

            var process = new UiShellCommand( Interpreter, Progress );

            process.Environment[ "Configuration" ] = BuildDefinition.Configuration;
            process.Environment[ "Platform" ] = BuildDefinition.Platform;
            process.Environment[ "OutputPath" ] = BuildDefinition.GetOutputPath();
            process.Environment[ "ProjectRoot" ] = BuildDefinition.RepositoryRoot;
            process.Environment[ "SolutionFile" ] = BuildDefinition.GetSolutionPath();
            process.Environment[ "NUnitPath" ] = Path.GetDirectoryName( BuildDefinition.TestRunnerExecutable );
            process.Environment[ "TestAssemblyPattern" ] =  BuildDefinition.TestAssemblyPattern;

            var compiledArguments = CompileScriptArgumentsInternal( script, target, args ).ToArray();

            process.Execute( compiledArguments );

            return process.ExitCode == 0;
        }

        protected abstract IEnumerable<string> CompileScriptArgumentsInternal( string script, string target, string[] args );
    }
}
