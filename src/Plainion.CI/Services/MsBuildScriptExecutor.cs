using System;
using System.Collections.Generic;
using System.IO;

namespace Plainion.CI.Services
{
    class MsBuildScriptExecutor : AbstractScriptExecutor
    {
        public MsBuildScriptExecutor( BuildDefinition buildDefinition, IProgress<string> progress )
            : base( buildDefinition, progress )
        {
        }

        protected override string Interpreter
        {
            get { return @"C:\Program Files (x86)\MSBuild\12.0\Bin\MSBuild.exe"; }
        }

        protected override IEnumerable<string> ValidScriptExtensions
        {
            get
            {
                yield return ".msbuild";
                yield return ".targets";
                yield return ".sln";
                yield return ".csproj";
            }
        }

        protected override IEnumerable<string> CompileScriptArgumentsInternal( string script, string target, string[] args )
        {
            yield return "/m";

            if( target != null )
            {
                yield return "/t:" + target;
            }

            // "OutputPath" has to be overwritten on command line
            yield return "/p:OutputPath=" + BuildDefinition.GetOutputPath();

            foreach( var arg in args )
            {
                yield return arg;
            }

            yield return script;
        }
    }
}
