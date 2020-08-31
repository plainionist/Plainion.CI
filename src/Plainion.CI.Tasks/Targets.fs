// TODO: temporary solution to migrate to FAKE 5 (.net core)
namespace Plainion.CI

open System
open System.IO
open Plainion.CI
open Plainion.CI.Tasks
open Fake.Core
open Fake.IO
open Fake.IO.FileSystemOperators

[<AutoOpen>]
module Common =
    let getProperty name =
        match name |> Environment.environVarOrNone with
        | Some x -> x
        | None -> failwithf "Property not found: %s" name

    /// get environment variable given by Plainion.CI engine
    let (!%) = getProperty

    let toolsHome = getProperty "ToolsHome"

    let buildDefinition = BuildDefinitionSerializer.TryDeserialize( !%"BuildDefinitionFile" )
    let buildRequest = BuildRequestSerializer.Deserialize()

    let projectRoot = buildDefinition.RepositoryRoot
    let outputPath = buildDefinition.GetOutputPath()

    let projectName = Path.GetFileNameWithoutExtension(buildDefinition.GetSolutionPath())

[<AutoOpen>]
module Impl = 
    let changeLogFile = projectRoot </> "ChangeLog.md"

    let private changeLog = lazy ( match File.exists changeLogFile with
                                   | true -> changeLogFile |> ReleaseNotes.load |> Some
                                   | false -> None
                                 )

    let getChangeLog() = 
        match changeLog.Value with
        | Some cl -> cl
        | None -> failwith "No ChangeLog.md found in project root"

    let private assemblyProjects = lazy (PMsBuild.getAssemblyProjectMap buildDefinition)

    let getAssemblyProjectMap() =
        assemblyProjects.Value

module PZip =
    let GetReleaseFile() = PZip.GetReleaseFile getChangeLog projectName outputPath
    let PackRelease() = PZip.PackRelease getChangeLog projectName outputPath

module PNuGet =
    let Pack nuspec packageOut files = PNuGet.Pack getChangeLog getAssemblyProjectMap projectName outputPath nuspec packageOut files
    let PublishPackage packageName packageOut = PNuGet.PublishPackage getChangeLog projectRoot packageName packageOut

    /// Publishes the NuGet package specified by packageOut, projectName and current version of ChangeLog.md
    /// to NuGet (https://www.nuget.org/api/v2/package)              
    let Publish packageOut = PublishPackage projectName packageOut

module PGitHub =
    let Release files = PGitHub.Release getChangeLog buildDefinition projectRoot projectName files

module Runtime =
    open Fake.Core.TargetOperators

    let ScriptRun() =
        Target.create "All" (fun _ ->
            Trace.trace "--- Plainion.CI - DONE ---"
        )

        Target.create "Clean" (fun _ ->
            Shell.cleanDir outputPath
        )

        Target.create "Build" (fun _ ->
            PMsBuild.Build buildDefinition outputPath
        )

        Target.create "RunTests" (fun _ ->
            PNUnit.RunTests buildDefinition projectRoot outputPath
        )

        Target.create "GenerateApiDoc" (fun _ ->
            PApiDoc.Generate getAssemblyProjectMap buildDefinition projectRoot outputPath
        )

        Target.create "Commit" (fun _ ->
            PGit.Commit buildDefinition projectRoot buildRequest
        )

        Target.create "Push" (fun _ ->
            PGit.Push buildDefinition projectRoot 
        )

        Target.create "AssemblyInfo" (fun _ ->
            PAssemblyInfoFile.Generate getChangeLog projectRoot projectName
        )

        Target.create "CreatePackage" (fun _ ->
            PPackaging.CreatePackage buildDefinition projectRoot outputPath
        )

        Target.create "DeployPackage" (fun _ ->
            PPackaging.DeployPackage buildDefinition projectRoot outputPath
        )

        Target.create "PublishPackage" (fun _ ->
            PPackaging.PublishPackage buildDefinition projectRoot outputPath
        )

        "Clean"
            =?> ("AssemblyInfo", changeLogFile |> File.Exists)
            ==> "Build"
            =?> ("GenerateApiDoc", buildDefinition.GenerateAPIDoc)
            =?> ("RunTests", buildDefinition.RunTests)
            =?> ("Commit", buildDefinition.CheckIn)
            =?> ("Push", buildDefinition.Push)
            =?> ("CreatePackage", buildDefinition.CreatePackage)
            =?> ("DeployPackage", buildDefinition.DeployPackage)
            =?> ("PublishPackage", buildDefinition.PublishPackage)
            ==> "All"
            |> ignore
    
        Target.runOrDefault "All"
