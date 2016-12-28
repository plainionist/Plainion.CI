using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Plainion.CI.Services.SourceControl;

namespace Plainion.CI.Services
{
    [Export]
    class BuildService : IDisposable
    {
        private ISourceControl mySourceControl;

        [ImportingConstructor]
        public BuildService( ISourceControl sourceControl )
        {
            mySourceControl = sourceControl;
        }

        public void Dispose()
        {
            if( BuildDefinition != null && BuildDefinition.RepositoryRoot != null )
            {
                BuildDefinitionSerializer.Serialize( BuildDefinition );
            }
        }

        public BuildDefinition BuildDefinition { get; private set; }

        public event Action BuildDefinitionChanged;

        public void InitializeBuildDefinition( string buildDefinitionFile )
        {
            BuildDefinition = BuildDefinitionSerializer.TryDeserialize( buildDefinitionFile );

            if( BuildDefinitionChanged != null )
            {
                BuildDefinitionChanged();
            }
        }

        public Task<bool> ExecuteAsync( BuildRequest request, IProgress<string> progress )
        {
            Contract.Invariant( BuildDefinition != null, "BuildDefinition not loaded" );

            request.Files = request.Files
                .Concat( new[] { BuildDefinitionSerializer.GetLocation( BuildDefinition ) } )
                .Distinct()
                .ToArray();

            BuildRequestSerializer.Serialize( request );

            // save all settings and parameters to be read by FAKE
            BuildDefinitionSerializer.Serialize( BuildDefinition );

            var buildDef = Objects.Clone( BuildDefinition );
            var toolsHome = Path.GetDirectoryName( GetType().Assembly.Location );

            return Task<bool>.Run( () =>
            {
                try
                {
                    var executor = new FakeScriptExecutor( buildDef, progress );
                    var success = executor.Execute( Path.Combine( toolsHome, "bits", "Workflow.fsx" ), "default" );

                    if( success )
                    {
                        progress.Report( "--- WORKFLOW SUCCEEDED ---" );
                    }
                    else
                    {
                        progress.Report( "--- WORKFLOW FAILED ---" );
                    }

                    return success;
                }
                catch( Exception ex )
                {
                    progress.Report( "ERROR: " + ex.Message );
                    progress.Report( "--- WORKFLOW FAILED ---" );
                    return false;
                }
            } );
        }
    }
}
