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
    let projectName = buildDefinition.GetProjectName()

module PZip =
    let GetReleaseFile() = PPackaging.API.GetReleaseFile projectRoot projectName outputPath
    let PackRelease() = buildDefinition |> PPackaging.PackReleaseRequest.Create |> PPackaging.PackRelease

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
    let Release files = (buildDefinition, files) |> PGitHub.GitHubReleaseRequest.Create |> PGitHub.Release

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
            (buildDefinition,buildRequest) |> PGit.GitCommitRequest.Create |> PGit.Commit 
        )

        Target.create "Push" (fun _ ->
            buildDefinition |> PGit.GitPushRequest.Create |> PGit.Push 
        )

        Target.create "UpdateAssemblyInfo" (fun _ ->
            buildDefinition |> PAssemblyInfoFile.AssemblyInfoFileRequest.Create |> PAssemblyInfoFile.Generate 
        )

        Target.create "CreatePackage" (fun _ ->
            (buildDefinition, buildDefinition.PackagingScript, buildDefinition.CreatePackageArguments)
            |> Extensions.ExecExtensionRequest.Create |> Extensions.Exec
        )

        Target.create "DeployPackage" (fun _ ->
            (buildDefinition, buildDefinition.PackagingScript, buildDefinition.DeployPackageArguments)
            |> Extensions.ExecExtensionRequest.Create |> Extensions.Exec
        )

        Target.create "PublishPackage" (fun _ ->
            (buildDefinition, buildDefinition.PackagingScript, buildDefinition.PublishPackageArguments)
            |> Extensions.ExecExtensionRequest.Create |> Extensions.Exec
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
