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

#load "GitHub.fsx"

open System
open System.IO
open Fake
open Plainion.CI
open Plainion.CI.Tasks

let getProperty name =
   match getBuildParamOrDefault name null with
   | null -> 
        match environVarOrNone name with
        | Some x -> x
        | None -> failwith "Property not found: " + name
   | x -> x

let getPropertyAndTrace name =
    let value = getProperty name
    name + "=" + value |> trace 
    value

/// get get environment variable given by Plainion.CI engine
let (!%) = getProperty

let toolsHome = getProperty "ToolsHome"

let buildDefinition = BuildDefinitionSerializer.TryDeserialize( !%"BuildDefinitionFile" )
let buildRequest = BuildRequestSerializer.Deserialize()

let projectRoot = buildDefinition.RepositoryRoot
let outputPath = buildDefinition.GetOutputPath()

let projectName = Path.GetFileNameWithoutExtension(buildDefinition.GetSolutionPath())
let changeLogFile = projectRoot </> "ChangeLog.md"

let private changeLog = lazy ( match fileExists changeLogFile with
                               | true -> ReleaseNotesHelper.LoadReleaseNotes changeLogFile |> Some
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

let setParams defaults =
    { defaults with
        Targets = ["Build"]
        Properties = [ "OutputPath", outputPath
                       "Configuration", buildDefinition.Configuration
                       "Platform", buildDefinition.Platform
                     ]
    }

module PZip =
    let private getReleaseName() =
        let release = getChangeLog()
        sprintf "%s-%s" projectName release.NugetVersion

    let GetReleaseFile () =
        outputPath </> ".." </> (sprintf "%s.zip" (getReleaseName()))

    /// Creates a zip from all content of the outputpath with current version backed in
    let PackRelease() = 
        let zip = GetReleaseFile()
        let releaseName = getReleaseName()

        !! ( outputPath </> "**/*.*" )
        |> Zip outputPath zip

module PNuGet =
    /// Creates a nuget package with the given files and nuspec at the packageOut folder.
    /// Version is taken from changelog.md
    let Pack nuspec packageOut files =
        let release = getChangeLog()
        
        CreateDir packageOut
        CleanDir packageOut

        let assemblies = 
            files 
            |> Seq.map(fun (source,target,exclude) -> source)
            |> Seq.collect(fun pattern -> !! (outputPath </> pattern))
            |> Seq.map filename
            |> List.ofSeq

        assemblies
        |> Seq.iter( fun a -> trace (sprintf "Adding file %s to package" a))

        let dependencies =
            getAssemblyProjectMap()
            |> Seq.filter(fun e -> assemblies |> List.exists ((=)e.Key))
            |> Seq.map(fun e -> (directory e.Value) </> "packages.config")
            |> Seq.collect(fun x -> x |> getDependencies )
            |> Seq.distinct
            |> List.ofSeq

        dependencies
        |> Seq.iter( fun d -> trace (sprintf "Package dependency detected: %A" d))

        nuspec 
        |> NuGet (fun p ->  {p with OutputPath = packageOut
                                    WorkingDir = outputPath
                                    Project = projectName
                                    Dependencies = dependencies
                                    Version = release.AssemblyVersion
                                    ReleaseNotes = release.Notes 
                                                   |> String.concat Environment.NewLine
                                    Files = files }) 
    
    /// Publishes the NuGet package specified by packageOut, projectName and current version of ChangeLog.md
    /// to nuget (https://www.nuget.org/api/v2/package)              
    let PublishPackage packageName packageOut =
        let release = getChangeLog()

        NuGetPublish  (fun p -> {p with OutputPath = packageOut
                                        WorkingDir = projectRoot
                                        Project = packageName
                                        Version = release.AssemblyVersion
                                        PublishUrl = "https://www.nuget.org/api/v2/package"
                                        Publish = true }) 

    /// Publishes the NuGet package specified by packageOut, projectName and current version of ChangeLog.md
    /// to nuget (https://www.nuget.org/api/v2/package)              
    let Publish packageOut =
        PublishPackage projectName packageOut

module PGitHub =
    open Fake.Git
    open Plainion.CI.Tasks

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
    
        // release on github
        
        let releaseNotes =  release.Notes 
                            |> List.ofSeq

        PGitHub.createDraft user pwd projectName release.NugetVersion (release.SemVer.PreRelease <> None) releaseNotes 
        |> PGitHub.uploadFiles files  
        |> PGitHub.releaseDraft
        |> Async.RunSynchronously

