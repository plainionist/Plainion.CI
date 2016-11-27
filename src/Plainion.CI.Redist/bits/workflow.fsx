#load "Targets.fsx"

open System.IO
open Fake
open Settings

"Clean"
    ==> "RestoreNugetPackages"
    =?> ("AssemblyInfo", releaseNotesFile |> File.Exists)
    ==> "Build"
    =?> ("GenerateApiDoc", buildDefinition.GenerateAPIDoc)
    =?> ("RunNUnitTests", buildDefinition.RunTests)
    =?> ("Commit", buildDefinition.CheckIn)
    =?> ("Push", buildDefinition.Push)
    =?> ("CreatePackage", buildDefinition.CreatePackage)
    =?> ("DeployPackage", buildDefinition.DeployPackage)
    ==> "Default"

RunTargetOrDefault "Default"
