using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Plainion.CI
{
    public class BuildDefinitionSerializer
    {
        public static string GetLocation( BuildDefinition def )
        {
            return Path.Combine( def.RepositoryRoot, Path.GetFileName( def.RepositoryRoot ) + ".gc" );
        }

        public static void Serialize( BuildDefinition def )
        {
            var file = GetLocation( def );
            using( var writer = XmlWriter.Create( file ) )
            {
                var serializer = new DataContractSerializer( typeof( BuildDefinition ) );
                serializer.WriteObject( writer, def );
            }

            var userFile = file + "." + Environment.UserName;
            using( var writer = XmlWriter.Create( userFile ) )
            {
                var serializer = new DataContractSerializer( typeof( User ) );
                serializer.WriteObject( writer, def.User );
            }
        }

        /// <summary>
        /// Creates an empty instance if the given file does not exist.
        /// </summary>
        public static BuildDefinition TryDeserialize( string file )
        {
            if( string.IsNullOrEmpty( file ) || !File.Exists( file ) )
            {
                return CreateDefaultBuildDefinition();
            }

            BuildDefinition def = null;

            using( var reader = XmlReader.Create( file ) )
            {
                var serializer = new DataContractSerializer( typeof( BuildDefinition ) );
                def = ( BuildDefinition )serializer.ReadObject( reader );
            }

            var userFile = file + "." + Environment.UserName;
            if( File.Exists( userFile ) )
            {
                using( var reader = XmlReader.Create( userFile ) )
                {
                    var serializer = new DataContractSerializer( typeof( User ) );
                    def.User = ( User )serializer.ReadObject( reader );
                }
            }

            // ctor is not called on deserialization - but there might no or empty user file exist
            if( def.User == null )
            {
                def.User = new User();
            }

            def.RepositoryRoot = Path.GetDirectoryName( file );

            return def;
        }

        private static BuildDefinition CreateDefaultBuildDefinition()
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
    }
}
