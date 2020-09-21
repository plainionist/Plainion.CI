module Plainion.CI.Tasks.PNuGet

open System
open System.IO
open Fake.Core
open Fake.DotNet.NuGet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open PMsBuild

type NuGetPackRequest = {
    ProjectRoot : string
    SolutionPath : string
    ProjectName : string
    OutputPath : string
    NuSpecPath : string
    PackageOutputPath : string
    Files : (string * string option * string option) list
} 

/// Creates a NuGet package with the given files and NuSpec at the packageOut folder.
/// Version is taken from changelog.md
let Pack request =
    let release = request.ProjectRoot |> GetChangeLog 
        
    Directory.create request.PackageOutputPath
    Shell.cleanDir request.PackageOutputPath

    let assemblies = 
        request.Files 
        |> Seq.map(fun (source,_,_) -> source)
        |> Seq.collect(fun pattern -> !! (request.OutputPath </> pattern))
        |> Seq.map Path.GetFileName
        |> List.ofSeq

    assemblies
    |> Seq.iter( fun a -> Trace.trace (sprintf "Adding file %s to package" a))

    let dependencies =
        let getDependencies (project:VsProject) =
            let packagesConfig = project.Location |> Path.GetDirectoryName </> "packages.config"

            if packagesConfig |> File.exists then
                packagesConfig 
                |> Fake.DotNet.NuGet.NuGet.getDependencies
                |> List.map(fun d -> d.Id,d.Version.AsString)
            else
                project.PackageReferences
                |> List.map(fun d -> d.Name,d.Version)

        request.SolutionPath
        |> PMsBuild.API.GetProjects
        |> Seq.filter(fun e -> assemblies |> List.exists ((=)e.Assembly))
        |> Seq.collect getDependencies
        |> Seq.distinct
        |> List.ofSeq

    dependencies
    |> Seq.iter( fun d -> Trace.trace (sprintf "Package dependency detected: %A" d))

    request.NuSpecPath 
    |>  NuGet.NuGet (fun p ->  {p with OutputPath = request.PackageOutputPath
                                       WorkingDir = request.OutputPath
                                       Project = request.ProjectName
                                       Dependencies = dependencies 
                                       Version = release |> Option.map(fun x -> x.AssemblyVersion) |? defaultAssemblyVersion
                                       ReleaseNotes = release |> Option.map(fun x -> x.Notes |> String.concat Environment.NewLine) |? ""
                                       Files = request.Files }) 

type NuGetPublishRequest = {
    ProjectRoot : string
    PackageName : string
    PackageOutputPath : string
}

let PublishPackage request =
    let release = request.ProjectRoot |> GetChangeLog 

    NuGet.NuGetPublish (fun p -> {p with OutputPath = request.PackageOutputPath
                                         WorkingDir = request.ProjectRoot
                                         Project = request.PackageName
                                         Version = release |> Option.map(fun x -> x.AssemblyVersion) |? defaultAssemblyVersion
                                         PublishUrl = "https://www.nuget.org/api/v2/package"
                                         Publish = true }) 
