using System;
using System.Collections.Generic;
using System.IO;
using Plainion.CI.Model;

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
            }
        }

        protected override IEnumerable<string> CompileScriptArgumentsInternal( string script, string target, Dictionary<string, string> commonProperties, string[] args )
        {
            yield return "/m";

            if( target != null )
            {
                yield return "/t:" + target;
            }

            foreach( var prop in commonProperties )
            {
                yield return string.Format( "/p:{0}={1}", prop.Key, prop.Value );
            }

            foreach( var arg in args )
            {
                yield return arg;
            }

            yield return script;
        }
    }
}
