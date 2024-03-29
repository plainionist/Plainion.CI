﻿using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;

namespace Plainion.CI.Services
{
    [Export]
    class BuildService : IDisposable
    {
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
                    var interpreter = Path.Combine(toolsHome, "Plainion.CI.BuildHost.exe");

                    var process = new UiShellCommand(interpreter, progress);

                    // extend PATH so that FAKE targets can find the tools
                    process.Environment["PATH"] = Path.Combine(toolsHome)
                        + Path.PathSeparator + Path.Combine(toolsHome, "NuGet")
                        + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH");

                    process.Environment["ToolsHome"] = toolsHome;
                    process.Environment["BuildDefinitionFile"] = buildDefinitionFile;
                    process.Environment["ProjectRoot"] = repositoryRoot;
                    process.Environment["outputPath "] = outputPath;

                    process.Execute();

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
