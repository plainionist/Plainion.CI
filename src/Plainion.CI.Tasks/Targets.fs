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

    let getPropertyAndTrace name =
        let value = getProperty name
        name + "=" + value |> Trace.trace 
        value

    /// get environment variable given by Plainion.CI engine
    let (!%) = getProperty

    let toolsHome = getProperty "ToolsHome"

    let buildDefinition = BuildDefinitionSerializer.TryDeserialize( !%"BuildDefinitionFile" )
    let buildRequest = BuildRequestSerializer.Deserialize()

    let projectRoot = buildDefinition.RepositoryRoot
    let outputPath = buildDefinition.GetOutputPath()

    let projectName = Path.GetFileNameWithoutExtension(buildDefinition.GetSolutionPath())
    let changeLogFile = projectRoot </> "ChangeLog.md"

    let private changeLog = lazy ( match File.exists changeLogFile with
                                   | true -> changeLogFile |> ReleaseNotes.load |> Some
                                   | false -> None
                                 )

    /// Returns the parsed ChangeLog.md if exists
    let getChangeLog () = 
        match changeLog.Value with
        | Some cl -> cl
        | None -> failwith "No ChangeLog.md found in project root"

    let private assemblyProjects = lazy (PMsBuild.getAssemblyProjectMap buildDefinition)

    /// Returns a dictionary mapping assembly names to their project files based on the project solution
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

module Targets =
    let All = (fun _ ->
        Trace.trace "--- Plainion.CI - DONE ---"
    )

    let Clean = (fun _ ->
        Shell.cleanDir outputPath
    )

    let Build = (fun _ ->
        PMsBuild.Build buildDefinition outputPath
    )

    let RunTests = (fun _ ->
        PNUnit.RunTests buildDefinition projectRoot outputPath
    )

    let GenerateApiDoc = (fun _ ->
        PApiDoc.Generate getAssemblyProjectMap buildDefinition projectRoot outputPath
    )

    let Commit = (fun _ ->
        PGit.Commit buildDefinition projectRoot buildRequest
    )

    let Push = (fun _ ->
        PGit.Push buildDefinition projectRoot 
    )

    let AssemblyInfo = (fun _ ->
        PAssemblyInfoFile.Generate getChangeLog projectRoot projectName
    )

    let CreatePackage = (fun _ ->
        PPackaging.CreatePackage buildDefinition projectRoot outputPath
    )

    let DeployPackage = (fun _ ->
        PPackaging.DeployPackage buildDefinition projectRoot outputPath
    )

    let PublishPackage = (fun _ ->
        PPackaging.PublishPackage buildDefinition projectRoot outputPath
    )

module Runtime =
    open Fake.Core.TargetOperators

    let ScriptRun() =
        Target.create "All" Targets.All

        Target.create "Clean" Targets.Clean

        Target.create "Build" Targets.Build

        Target.create "RunTests" Targets.RunTests

        Target.create "GenerateApiDoc" Targets.GenerateApiDoc

        Target.create "Commit" Targets.Commit

        Target.create "Push" Targets.Push

        Target.create "AssemblyInfo" Targets.AssemblyInfo

        Target.create "CreatePackage" Targets.CreatePackage

        Target.create "DeployPackage" Targets.DeployPackage

        Target.create "PublishPackage" Targets.PublishPackage

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
