namespace Plainion.CI

open System.IO
open Plainion.CI
open Plainion.CI.Tasks
open Fake.Core
open Fake.IO

[<AutoOpen>]
module API =
    let getProperty name =
        match name |> Environment.environVarOrNone with
        | Some x -> x
        | None -> failwithf "Property not found: %s" name

[<AutoOpen>]
module Common =
    /// get environment variable given by Plainion.CI engine
    let (!%) = getProperty

    let toolsHome = getProperty "ToolsHome"

    let buildDefinition = BuildDefinitionSerializer.TryDeserialize( !%"BuildDefinitionFile" )
    let buildRequest = BuildRequestSerializer.Deserialize()

    let projectRoot = buildDefinition.RepositoryRoot
    let outputPath = buildDefinition.GetOutputPath()

    let projectName = Path.GetFileNameWithoutExtension(buildDefinition.GetSolutionPath())

module PZip =
    let GetReleaseFile() = PPackaging.GetReleaseFile projectRoot projectName outputPath
    let PackRelease() = PPackaging.PackRelease projectRoot projectName outputPath

module PNuGet =
    let Pack nuspec packageOut files = 
        buildDefinition 
        |> PNuGet.NuGetPackRequest.Create
        |> fun x -> 
            { x with
                NuSpecPath = nuspec
                PackageOutputPath = packageOut
                Files = files
            }
        |> PNuGet.Pack 
    let PublishPackage packageName packageOut = 
        buildDefinition 
        |> PNuGet.NuGetPublishRequest.Create
        |> fun x -> 
            { x with
                PackageName = packageName
                PackageOutputPath = packageOut
            }
        |> PNuGet.PublishPackage
    let Publish packageOut = PublishPackage projectName packageOut

module PGitHub =
    let Release files = PGitHub.Release buildDefinition projectRoot projectName files

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
            buildDefinition |> PMsBuild.BuildRequest.Create |> PMsBuild.Build
        )

        Target.create "RunTests" (fun _ ->
            buildDefinition |> PNUnit.RunTestsRequest.Create |> PNUnit.RunTests
        )

        Target.create "GenerateApiDoc" (fun _ ->
            buildDefinition |> PApiDoc.ApiDocRequest.Create |> PApiDoc.Generate
        )

        Target.create "Commit" (fun _ ->
            PGit.Commit buildDefinition projectRoot buildRequest
        )

        Target.create "Push" (fun _ ->
            PGit.Push buildDefinition projectRoot 
        )

        Target.create "UpdateAssemblyInfo" (fun _ ->
            buildDefinition |> PAssemblyInfoFile.AssemblyInfoFileRequest.Create |> PAssemblyInfoFile.Generate 
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
            ==> ("UpdateAssemblyInfo")
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
