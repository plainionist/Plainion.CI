using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml;
using Plainion.CI.Model;
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
            SaveBuildDefinitionOnDemand();
        }

        private void SaveBuildDefinitionOnDemand()
        {
            if( BuildDefinition == null || BuildDefinition.RepositoryRoot == null )
            {
                return;
            }

            var buildDefinitionFile = Path.Combine( BuildDefinition.RepositoryRoot, Path.GetFileName( BuildDefinition.RepositoryRoot ) + ".gc" );
            using( var writer = XmlWriter.Create( buildDefinitionFile ) )
            {
                var serializer = new DataContractSerializer( typeof( BuildDefinition ) );
                serializer.WriteObject( writer, BuildDefinition );
            }

            var userFile = buildDefinitionFile + "." + Environment.UserName;
            using( var writer = XmlWriter.Create( userFile ) )
            {
                var serializer = new DataContractSerializer( typeof( User ) );
                serializer.WriteObject( writer, BuildDefinition.User );
            }
        }

        public BuildDefinition BuildDefinition { get; private set; }

        public event Action BuildDefinitionChanged;

        public void InitializeBuildDefinition( string buildDefinitionFile )
        {
            if( string.IsNullOrEmpty( buildDefinitionFile ) || !File.Exists( buildDefinitionFile ) )
            {
                BuildDefinition = CreateDefaultBuildDefinition();
            }
            else
            {
                using( var reader = XmlReader.Create( buildDefinitionFile ) )
                {
                    var serializer = new DataContractSerializer( typeof( BuildDefinition ) );
                    BuildDefinition = ( BuildDefinition )serializer.ReadObject( reader );
                }

                var userFile = buildDefinitionFile + "." + Environment.UserName;
                if( File.Exists( userFile ) )
                {
                    using( var reader = XmlReader.Create( userFile ) )
                    {
                        var serializer = new DataContractSerializer( typeof( User ) );
                        BuildDefinition.User = ( User )serializer.ReadObject( reader );
                    }
                }

                // ctor is not called on deserialization - but there might no or empty user file exist
                if( BuildDefinition.User == null )
                {
                    BuildDefinition.User = new User();
                }
            }

            BuildDefinition.RepositoryRoot = Path.GetDirectoryName( buildDefinitionFile );

            if( BuildDefinitionChanged != null )
            {
                BuildDefinitionChanged();
            }
        }

        private BuildDefinition CreateDefaultBuildDefinition()
        {
            return new BuildDefinition
            {
                CheckIn = true,
                Configuration = "Debug",
                Platform = "Any CPU",
                RunTests = true,
                TestAssemblyPattern = "*Tests.dll",
                TestRunnerExecutable = @"\bin\NUnit\bin\nunit-console.exe",
            };
        }

        public Task<bool> ExecuteAsync( BuildRequest request, IProgress<string> progress )
        {
            Contract.Invariant( BuildDefinition != null, "BuildDefinition not loaded" );

            // save settings before running workflow to ensure that such changes get checked in as well
            SaveBuildDefinitionOnDemand();

            return new BuildWorkflow( mySourceControl, BuildDefinition, request )
                .ExecuteAsync( progress );
        }
    }
}
