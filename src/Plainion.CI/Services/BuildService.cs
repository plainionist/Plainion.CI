using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using Plainion.CI.Services.SourceControl;

namespace Plainion.CI.Services
{
    [Export]
    class BuildService : IDisposable
    {
        private ISourceControl mySourceControl;

        [ImportingConstructor]
        public BuildService(ISourceControl sourceControl)
        {
            mySourceControl = sourceControl;
        }

        public void Dispose()
        {
            if (BuildDefinition != null && BuildDefinition.RepositoryRoot != null)
            {
                BuildDefinitionSerializer.Serialize(BuildDefinition);
            }
        }

        public BuildDefinition BuildDefinition { get; private set; }

        public event Action BuildDefinitionChanged;

        public void InitializeBuildDefinition(string buildDefinitionFile)
        {
            BuildDefinition = BuildDefinitionSerializer.TryDeserialize(buildDefinitionFile);

            if (BuildDefinitionChanged != null)
            {
                BuildDefinitionChanged();
            }
        }

        public Task<bool> ExecuteAsync(BuildRequest request, IProgress<string> progress)
        {
            Contract.Invariant(BuildDefinition != null, "BuildDefinition not loaded");

            BuildRequestSerializer.Serialize(request);
            BuildDefinitionSerializer.Serialize(BuildDefinition);

            var toolsHome = Path.GetDirectoryName(GetType().Assembly.Location);
            var repositoryRoot = BuildDefinition.RepositoryRoot;
            var buildDefinitionFile = BuildDefinitionSerializer.GetLocation(BuildDefinition);
            var outputPath = BuildDefinition.GetOutputPath();

            return Task<bool>.Run(() =>
            {
                try
                {
                    var script = Path.Combine(toolsHome, "bits", "Workflow.fsx");

                    if (!Path.IsPathRooted(script))
                    {
                        script = Path.Combine(repositoryRoot, script);
                    }

                    var interpreter = Path.Combine(toolsHome, "FAKE", "fake.exe");

                    var process = new UiShellCommand(interpreter, progress);

                    // extend PATH so that FAKE targets can find the tools
                    process.Environment["PATH"] = Path.Combine(toolsHome, "FAKE")
                        + Path.PathSeparator + Path.Combine(toolsHome, "NuGet")
                        + Path.PathSeparator + @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin" +
                        + Path.PathSeparator + @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\MSBuild\Current\Bin" +
                        + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH");

                    process.Environment["ToolsHome"] = toolsHome;
                    process.Environment["BuildDefinitionFile"] = buildDefinitionFile;
                    process.Environment["ProjectRoot"] = repositoryRoot;
                    process.Environment["outputPath "] = outputPath;

                    var compiledArguments = new[] {
                        script,
                        "All",
                        "--fsiargs \"--define:FAKE\"",
                        "--removeLegacyFakeWarning",
                    };

                    progress.Report($"fake.exe {string.Join(" ", compiledArguments)}");

                    process.Execute(compiledArguments);

                    var success = process.ExitCode == 0;

                    if (success)
                    {
                        progress.Report("--- WORKFLOW SUCCEEDED ---");
                    }
                    else
                    {
                        progress.Report("--- WORKFLOW FAILED ---");
                    }

                    return success;
                }
                catch (Exception ex)
                {
                    progress.Report("ERROR: " + ex.Message);
                    progress.Report("--- WORKFLOW FAILED ---");
                    return false;
                }
            });
        }
    }
}
