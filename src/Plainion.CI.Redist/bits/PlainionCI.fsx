module PlainionCI

#if FAKE
#r "../FAKE/FakeLib.dll"
#r "../Plainion.CI.Core.dll"
#r "../Plainion.Core.dll"
#r "../Plainion.CI.Tasks.dll"
#else
#r "../../../bin/Debug/FAKE/FakeLib.dll"
#r "../../../bin/Debug/Plainion.Core.dll"
#r "../../../bin/Debug/Plainion.CI.Core.dll"
#r "../../../bin/Debug/Plainion.CI.Tasks.dll"
#endif

open System
open System.IO
open Plainion.CI
open Plainion.CI.Tasks
open Fake.Core
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.DotNet

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


let private assemblyProjects = lazy (   let projects = PMsBuild.GetProjectFiles(buildDefinition.GetSolutionPath())
                                        projects
                                        |> Seq.map PMsBuild.LoadProject
                                        |> Seq.map(fun proj -> proj.Assembly, proj.Location)
                                        |> dict
                                    )

/// Returns a dictionary mapping assembly names to their project files based on the project solution
let getAssemblyProjectMap() =
    assemblyProjects.Value

module PZip =
    let private getReleaseName() =
        let release = getChangeLog()
        sprintf "%s-%s" projectName release.NugetVersion

    let GetReleaseFile () =
        outputPath </> ".." </> (sprintf "%s.zip" (getReleaseName()))

    /// Creates a zip from all content of the OutputPath with current version backed in
    let PackRelease() = 
        let zip = GetReleaseFile()
        let releaseName = getReleaseName()

        !! ( outputPath </> "**/*.*" )
        |> Zip.zip outputPath zip

module PNuGet =
    open Fake.DotNet.NuGet

    /// Creates a NuGet package with the given files and NuSpec at the packageOut folder.
    /// Version is taken from changelog.md
    let Pack nuspec packageOut files =
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
            let getDependencies projectFile =
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
                                           Version = release.AssemblyVersion
                                           ReleaseNotes = release.Notes 
                                                          |> String.concat Environment.NewLine
                                           Files = files }) 
    
    /// Publishes the NuGet package specified by packageOut, projectName and current version of ChangeLog.md
    /// to NuGet (https://www.nuget.org/api/v2/package)              
    let PublishPackage packageName packageOut =
        let release = getChangeLog()

        NuGet.NuGetPublish (fun p -> {p with OutputPath = packageOut
                                             WorkingDir = projectRoot
                                             Project = packageName
                                             Version = release.AssemblyVersion
                                             PublishUrl = "https://www.nuget.org/api/v2/package"
                                             Publish = true }) 

    /// Publishes the NuGet package specified by packageOut, projectName and current version of ChangeLog.md
    /// to NuGet (https://www.nuget.org/api/v2/package)              
    let Publish packageOut =
        PublishPackage projectName packageOut

module PGitHub =
    open Plainion.CI.Tasks
    open Fake.Tools.Git

    /// Publishes a new release to GitHub with the current version of ChangeLog.md and
    /// the given files
    let Release files =
        if buildDefinition.User.Password = null then
            failwith "!! NO PASSWORD PROVIDED !!"
    
        let release = getChangeLog()

        let user = buildDefinition.User.Login
        let pwd = buildDefinition.User.Password.ToUnsecureString()

        try
            Branches.deleteTag "" release.NugetVersion
        with | _ -> ()
        
        Branches.tag "" release.NugetVersion
        PGit.Push projectRoot (user, pwd)
    
        // release on GitHub
        
        let releaseNotes =  release.Notes 
                            |> List.ofSeq

        PGitHub.createDraft user pwd projectName release.NugetVersion (release.SemVer.PreRelease <> None) releaseNotes 
        |> PGitHub.uploadFiles files  
        |> PGitHub.releaseDraft
        |> Async.RunSynchronously

