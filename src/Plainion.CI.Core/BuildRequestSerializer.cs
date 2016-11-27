using System.IO;
using System.Runtime.Serialization;
using System.Xml;

namespace Plainion.CI
{
    public class BuildRequestSerializer
    {
        private static string GetFile()
        {
            return Path.Combine( Path.GetTempPath(), "Plainion.CI.Build.Request.xml" );
        }

        public static string Serialize( BuildRequest request )
        {
            var file = GetFile();

            using( var writer = XmlWriter.Create( file ) )
            {
                var serializer = new DataContractSerializer( typeof( BuildRequest ) );
                serializer.WriteObject( writer, request );
            }

            return file;
        }

        public static BuildRequest Deserialize()
        {
            var file = GetFile();

            if( string.IsNullOrEmpty( file ) || !File.Exists( file ) )
            {   
                throw new FileNotFoundException( file );
            }

            using( var reader = XmlReader.Create( file ) )
            {
                var serializer = new DataContractSerializer( typeof( BuildRequest ) );
                return ( BuildRequest )serializer.ReadObject( reader );
            }
        }
    }
}
