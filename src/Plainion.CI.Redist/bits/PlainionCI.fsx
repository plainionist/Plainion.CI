module PlainionCI

#if FAKE
#r "../FAKE/FakeLib.dll"
#r "../Plainion.CI.Core.dll"
#r "../Plainion.Core.dll"
#else
#r "../../../bin/Debug/FAKE/FakeLib.dll"
#r "../../../bin/Debug/Plainion.Core.dll"
#r "../../../bin/Debug/Plainion.CI.Core.dll"
#endif

open System
open System.IO
open Fake
open Plainion.CI

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
let releaseNotesFile = projectRoot </> "ChangeLog.md"

let setParams defaults =
    { defaults with
        Targets = ["Build"]
        Properties = [ "OutputPath", outputPath
                       "Configuration", buildDefinition.Configuration
                       "Platform", buildDefinition.Platform
                     ]
    }

/// Creates a nuget package with the given files and nuspec at the packageOut folder.
/// Version is taken from changelog.md
let CreateNuGetPackage nuspec packageOut files =
    let release = ReleaseNotesHelper.LoadReleaseNotes releaseNotesFile

    CreateDir packageOut
    CleanDir packageOut

    nuspec 
    |> NuGet (fun p ->  {p with OutputPath = packageOut
                                WorkingDir = outputPath
                                Project = projectName
                                Version = release.AssemblyVersion
                                ReleaseNotes = release.Notes 
                                               |> Seq.map ((+) "- ")
                                               |> String.concat Environment.NewLine
                                Files = files }) 
                    
let PublishNuGetPackage packageOut =
    let release = ReleaseNotesHelper.LoadReleaseNotes releaseNotesFile

    NuGetPublish  (fun p -> {p with OutputPath = packageOut
                                    WorkingDir = projectRoot
                                    Project = projectName
                                    Version = release.AssemblyVersion
                                    PublishUrl = "https://www.nuget.org/api/v2/package"
                                    Publish = true }) 

let PublishReleaseOnGitHub () =
    //https://github.com/fsprojects/ProjectScaffold/blob/master/build.template
    ()