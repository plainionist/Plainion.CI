module Plainion.CI.Tasks.PZip

open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators

let private getReleaseName (getChangeLog:GetChangeLog) projectName =
    let release = getChangeLog()
    sprintf "%s-%s" projectName release.NugetVersion

let GetReleaseFile (getChangeLog:GetChangeLog) projectName outputPath =
    let releaseName = getReleaseName getChangeLog projectName
    outputPath </> ".." </> (sprintf "%s.zip" releaseName)

/// Creates a zip from all content of the OutputPath with current version backed in
let PackRelease (getChangeLog:GetChangeLog) projectName outputPath = 
    let zip = GetReleaseFile getChangeLog projectName outputPath

    !! ( outputPath </> "**/*.*" )
    |> Zip.zip outputPath zip
