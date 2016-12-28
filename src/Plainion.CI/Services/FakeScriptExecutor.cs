using System;
using System.Collections.Generic;
using System.IO;

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

        protected override IEnumerable<string> CompileScriptArgumentsInternal( string script, string[] args )
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
