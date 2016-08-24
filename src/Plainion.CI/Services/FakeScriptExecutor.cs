using System;
using System.Collections.Generic;
using System.IO;
using Plainion.CI.Model;

namespace Plainion.CI.Services
{
    class FakeScriptExecutor : AbstractScriptExecutor
    {
        public FakeScriptExecutor( BuildDefinition buildDefinition, IProgress<string> progress )
            : base( buildDefinition, progress )
        {
        }

        protected override string Interpreter
        {
            get
            {
                var home = Path.GetDirectoryName( GetType().Assembly.Location );
                return Path.Combine( home, "FAKE", "fake.exe" );
            }
        }

        protected override IEnumerable<string> ValidScriptExtensions
        {
            get { yield return ".fsx"; }
        }

        protected override IEnumerable<string> CompileScriptArgumentsInternal( string script, string target, string[] args )
        {
            yield return script;

            yield return target;

            foreach( var arg in args )
            {
                yield return arg;
            }
        }
    }
}
