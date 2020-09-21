namespace Plainion.CI

open Fake.Core
open Fake.IO
open Plainion.CI
open Plainion.CI.Tasks

[<AutoOpen>]
module Common =
    let getProperty name =
        match name |> Environment.environVarOrNone with
        | Some x -> x
        | None -> failwithf "Property not found: %s" name
    let (!%) = getProperty

[<AutoOpen>]
module Variables =
    open Fake.IO.FileSystemOperators

    let toolsHome = getProperty "ToolsHome"

    let buildDefinition = BuildDefinitionSerializer.TryDeserialize( !%"BuildDefinitionFile" )
    let buildRequest = BuildRequestSerializer.Deserialize()

    let projectRoot = buildDefinition.RepositoryRoot
    let outputPath = buildDefinition.GetOutputPath()
    let projectName = buildDefinition.GetProjectName()

    open PMsBuild
    type MsBuildRequest with
        static member Default =
            {
                MsBuildRequest.SolutionPath = buildDefinition.GetSolutionPath()
                OutputPath = buildDefinition.GetOutputPath()
                Configuration = buildDefinition.Configuration
                Platform = buildDefinition.Platform
            }

    open PNUnit
    type RunTestsRequest with 
        static member Default =
            {
                TestRunnerExecutable = buildDefinition.TestRunnerExecutable
                TestAssemblyPattern = buildDefinition.TestAssemblyPattern
                ProjectRoot = buildDefinition.RepositoryRoot
                OutputPath = buildDefinition.GetOutputPath()
            }

    open PNuGet
    type NuGetPackRequest with
        static member Default =
            {
                ProjectRoot = buildDefinition.RepositoryRoot
                SolutionPath = buildDefinition.GetSolutionPath()
                ProjectName = buildDefinition.GetProjectName()
                OutputPath = buildDefinition.GetOutputPath()
                NuSpecPath = buildDefinition.RepositoryRoot </> "build" </> (buildDefinition.GetProjectName() |> sprintf "%s.nuspec")
                PackageOutputPath = buildDefinition.RepositoryRoot </> "pkg"
                Files = []
            }
    type NuGetPublishRequest with 
        static member Default =
            {
                ProjectRoot = buildDefinition.RepositoryRoot
                PackageName = buildDefinition.GetProjectName()
                PackageOutputPath = buildDefinition.RepositoryRoot </> "pkg"
            }

    open PApiDoc
    type ApiDocRequest with 
        static member Default =
            {
                ProjectRoot = buildDefinition.RepositoryRoot
                OutputPath = buildDefinition.GetOutputPath()
                ApiDocGenExecutable = buildDefinition.ApiDocGenExecutable
                ApiDocGenArguments = buildDefinition.ApiDocGenArguments
                TestAssemblyPattern = buildDefinition.TestAssemblyPattern
                SolutionPath = buildDefinition.GetSolutionPath()
            }

    open PAssemblyInfoFile
    type AssemblyInfoFileRequest with 
        static member Default =
            {
                ProjectRoot = buildDefinition.RepositoryRoot
                ProjectName = buildDefinition.GetProjectName()
            }

    open PGit
    type GitPushRequest with 
        static member Default =
            {
                ProjectRoot = buildDefinition.RepositoryRoot
                User = buildDefinition.User
            }
    type GitCommitRequest with 
        static member Default =
            {
                ProjectRoot = buildDefinition.RepositoryRoot
                User = buildDefinition.User
                CheckInComment = buildRequest.CheckInComment
                FilesExcludedFromCheckIn = buildRequest.FilesExcludedFromCheckIn
            }
    
    open PGitHub
    type GitHubReleaseRequest with 
        static member Default =
            {
                ProjectRoot = buildDefinition.RepositoryRoot
                User = buildDefinition.User
                ProjectName = buildDefinition.GetProjectName()
                Files = []
            }

    open PPackaging
    type PackReleaseRequest with 
        static member Default =
            {
                ProjectRoot = buildDefinition.RepositoryRoot
                ProjectName = buildDefinition.GetProjectName()
                OutputPath = buildDefinition.GetOutputPath()
            }

    open Extensions
    type ExecExtensionRequest with 
        static member Default =
            {
                ProjectRoot = buildDefinition.RepositoryRoot
                OutputPath = buildDefinition.GetOutputPath()
                ExtensionPath = ""
                ExtensionArguments = ""
            }

module PZip =
    let GetReleaseFile() = PPackaging.API.GetReleaseFile projectRoot projectName outputPath
    let PackRelease() = PPackaging.PackReleaseRequest.Default |> PPackaging.PackRelease

module PNuGet =
    let Pack nuspec packageOut files = 
        PNuGet.NuGetPackRequest.Default
        |> fun x -> 
            { x with
                NuSpecPath = nuspec
                PackageOutputPath = packageOut
                Files = files
            }
        |> PNuGet.Pack 
    let PublishPackage packageName packageOut = 
        PNuGet.NuGetPublishRequest.Default
        |> fun x -> 
            { x with
                PackageName = packageName
                PackageOutputPath = packageOut
            }
        |> PNuGet.PublishPackage
    let Publish packageOut = PublishPackage projectName packageOut

module PGitHub =
    let Release files = { PGitHub.GitHubReleaseRequest.Default with Files = files }|> PGitHub.Release

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
            PMsBuild.MsBuildRequest.Default |> PMsBuild.Build
        )

        Target.create "RunTests" (fun _ ->
            PNUnit.RunTestsRequest.Default |> PNUnit.RunTests
        )

        Target.create "GenerateApiDoc" (fun _ ->
            PApiDoc.ApiDocRequest.Default |> PApiDoc.Generate
        )

        Target.create "Commit" (fun _ ->
            PGit.GitCommitRequest.Default |> PGit.Commit 
        )

        Target.create "Push" (fun _ ->
            PGit.GitPushRequest.Default |> PGit.Push 
        )

        Target.create "UpdateAssemblyInfo" (fun _ ->
            PAssemblyInfoFile.AssemblyInfoFileRequest.Default |> PAssemblyInfoFile.Generate 
        )

        Target.create "CreatePackage" (fun _ ->
            { Extensions.ExecExtensionRequest.Default with
                ExtensionPath = buildDefinition.PackagingScript
                ExtensionArguments = buildDefinition.CreatePackageArguments } |> Extensions.Exec
        )

        Target.create "DeployPackage" (fun _ ->
            { Extensions.ExecExtensionRequest.Default with
                ExtensionPath = buildDefinition.PackagingScript
                ExtensionArguments = buildDefinition.DeployPackageArguments } |> Extensions.Exec
        )

        Target.create "PublishPackage" (fun _ ->
            { Extensions.ExecExtensionRequest.Default with
                ExtensionPath = buildDefinition.PackagingScript
                ExtensionArguments = buildDefinition.PublishPackageArguments } |> Extensions.Exec
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
