module Plainion.CI.Tasks.PPackaging

open Plainion.CI
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators

[<AutoOpen>]
module private Impl = 
    let getReleaseName projectRoot projectName =
        let createName version = sprintf "%s-%s" projectName version
        projectRoot 
        |> GetChangeLog 
        |> Option.map(fun release -> createName release.NugetVersion)
        |> Option.defaultValue (createName defaultAssemblyVersion)

module API =
    let GetReleaseFile projectRoot projectName outputPath =
        let releaseName = getReleaseName projectRoot projectName
        outputPath </> ".." </> (sprintf "%s.zip" releaseName)

type PackReleaseRequest = {
    ProjectRoot : string
    ProjectName : string
    OutputPath : string
} with 
    static member Create (def:BuildDefinition) =
        {
            ProjectRoot = def.RepositoryRoot
            ProjectName = def.GetProjectName()
            OutputPath = def.GetOutputPath()
        }

/// Creates a zip from all content of the OutputPath with current version backed in
let PackRelease request = 
    let zip = API.GetReleaseFile request.ProjectRoot request.ProjectName request.OutputPath

    !! ( request.OutputPath </> "**/*.*" )
    |> Zip.zip request.OutputPath zip
