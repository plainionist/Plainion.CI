module Plainion.CI.Tasks.PNuGet

open System
open System.IO
open Fake.Core
open Fake.DotNet.NuGet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators

/// Creates a NuGet package with the given files and NuSpec at the packageOut folder.
/// Version is taken from changelog.md
let Pack (getChangeLog:GetChangeLog) (getAssemblyProjectMap:GetAssemblyProjectMap) projectName outputPath nuspec packageOut files =
    let release = getChangeLog()
        
    Directory.create packageOut
    Shell.cleanDir packageOut

    let assemblies = 
        files 
        |> Seq.map(fun (source,_,_) -> source)
        |> Seq.collect(fun pattern -> !! (outputPath </> pattern))
        |> Seq.map Path.GetFileName
        |> List.ofSeq

    assemblies
    |> Seq.iter( fun a -> Trace.trace (sprintf "Adding file %s to package" a))

    let dependencies =
        let getDependencies (projectFile:string) =
            let packagesConfig = projectFile |> Path.GetDirectoryName </> "packages.config"

            if packagesConfig |> File.exists then
                packagesConfig 
                |> Fake.DotNet.NuGet.NuGet.getDependencies
                |> List.map(fun d -> d.Id,d.Version.AsString)
            else
                //     <PackageReference Include="System.ComponentModel" Version="4.3.0" />
                projectFile 
                |> PMsBuild.GetPackageReferences

        getAssemblyProjectMap()
        |> Seq.filter(fun e -> assemblies |> List.exists ((=)e.Key))
        |> Seq.collect(fun e -> e.Value |> getDependencies)
        |> Seq.distinct
        |> List.ofSeq

    dependencies
    |> Seq.iter( fun d -> Trace.trace (sprintf "Package dependency detected: %A" d))

    nuspec 
    |>  NuGet.NuGet (fun p ->  {p with OutputPath = packageOut
                                       WorkingDir = outputPath
                                       Project = projectName
                                       Dependencies = dependencies 
                                       Version = release |> Option.map(fun x -> x.AssemblyVersion) |? defaultAssemblyVersion
                                       ReleaseNotes = release |> Option.map(fun x -> x.Notes |> String.concat Environment.NewLine) |? ""
                                       Files = files }) 

/// Publishes the NuGet package specified by packageOut, projectName and current version of ChangeLog.md
/// to NuGet (https://www.nuget.org/api/v2/package)              
let PublishPackage (getChangeLog:GetChangeLog) projectRoot packageName packageOut =
    let release = getChangeLog()

    NuGet.NuGetPublish (fun p -> {p with OutputPath = packageOut
                                         WorkingDir = projectRoot
                                         Project = packageName
                                         Version = release |> Option.map(fun x -> x.AssemblyVersion) |? defaultAssemblyVersion
                                         PublishUrl = "https://www.nuget.org/api/v2/package"
                                         Publish = true }) 
