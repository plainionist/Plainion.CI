using System;
using System.ComponentModel.Composition;
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

            // save all settings and parameters to be read by FAKE
            BuildDefinitionSerializer.Serialize( BuildDefinition );

            request.Files = request.Files
                .Concat( new[] { BuildDefinitionSerializer.GetLocation( BuildDefinition ) } )
                .Distinct()
                .ToArray();

            BuildRequestSerializer.Serialize( request );

            return new BuildWorkflow( mySourceControl, BuildDefinition, request )
                .ExecuteAsync( progress );
        }
    }
}
