using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Plainion.CI.Services.Msbuild
{
    public class NUnit : Task
    {
        [Required]
        public string NUnitConsole { get; set; }

        [Required]
        public string AssembliesPattern { get; set; }

        [Required]
        public string WorkingDirectory { get; set; }

        public override bool Execute()
        {
            Log.LogMessage( MessageImportance.Normal, "NUnit execution started" );

            var nunitProject = GenerateProject();
            if ( nunitProject == null )
            {
                Log.LogError( "No test assemblies found" );
                return false;
            }

            try
            {
                Contract.Requires( File.Exists( NUnitConsole ), "Runner executable not found: {0}", NUnitConsole );

                // shadowcopy is an issue if we load files during UT according to assembly location
                var info = new ProcessStartInfo( NUnitConsole, "/noshadow " + nunitProject );
                info.UseShellExecute = false;

                var process = Process.Start( info );
                process.WaitForExit();

                return process.ExitCode == 0;
            }
            catch ( Exception ex )
            {
                Log.LogError( ex.Message );
            }
            finally
            {
                File.Delete( nunitProject );
            }

            return false;
        }

        private string GenerateProject()
        {
            var testAssemblies = ResolveTestAssemblies();

            if ( !testAssemblies.Any() )
            {
                return null;
            }

            // assume shortest folder is the root folder
            var testFolder = testAssemblies
                .Select( path => Path.GetDirectoryName( path ) )
                .OrderBy( dir => dir.Length )
                .First();

            var project = new XElement( "NUnitProject",
                new XElement( "Settings",
                    new XAttribute( "activeconfig", "default" ),
                    new XAttribute( "appbase", testFolder ) ),
                new XElement( "Config", new XAttribute( "name", "default" ),
                    testAssemblies
                        .Select( assembly => new XElement( "assembly", new XAttribute( "path", assembly ) ) )
                    ) );

            var projectFile = Path.Combine( testFolder, "Plainion.gen.nunit" );
            using ( var writer = XmlWriter.Create( projectFile ) )
            {
                project.WriteTo( writer );
            }

            Log.LogMessage( MessageImportance.Low, "NUnit project written to: {0}", projectFile );

            return projectFile;
        }

        private IEnumerable<string> ResolveTestAssemblies()
        {
            var assemblyPatterns = AssembliesPattern.Split( new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries );

            return assemblyPatterns
                .SelectMany( pattern => ResolveFilePattern( pattern ) )
                .Distinct()
                .ToList();
        }

        private IEnumerable<string> ResolveFilePattern( string pattern )
        {
            var directory = Path.GetDirectoryName( pattern );
            if ( string.IsNullOrWhiteSpace( directory ) )
            {
                directory = Path.GetFullPath( WorkingDirectory );
            }

            var filePattern = Path.GetFileName( pattern );

            Log.LogMessage( MessageImportance.Normal, "Searching in {0} for {1}", directory, filePattern );

            var testAssemblies = Directory.GetFiles( directory, filePattern )
                .Where( file => Path.GetExtension( file ).Equals( ".dll", StringComparison.OrdinalIgnoreCase ) )
                .ToList();

            foreach ( var file in testAssemblies )
            {
                Log.LogMessage( MessageImportance.Normal, "  -> {0}", file );
            }

            return testAssemblies;
        }
    }
}
