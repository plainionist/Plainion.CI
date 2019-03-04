// load dependencies from source folder to allow bootstrapping
#r "../bin/Debug/FAKE/FakeLib.dll"
#load "../bin/Debug/bits/PlainionCI.fsx"

open Fake.Core
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open PlainionCI

Target.create "CreatePackage" (fun _ ->
    !! ( outputPath </> "*.*Tests.*" )
    ++ ( outputPath </> "*nunit*" )
    ++ ( outputPath </> "TestResult.xml" )
    |> File.deleteAll

    !! ( outputPath </> "Plainion.CI.Redist.*" )
    ++ ( outputPath </> "**/*.pdb" )
    |> File.deleteAll

    PZip.PackRelease()

    // create a dummy nuget package for testing
//    [
//        ("Plainion.CI*", Some "lib", None)
//    ]
//    |> PNuGet.Pack (projectRoot </> "build" </> "Dummy.nuspec") (projectRoot </> "pkg")
)

Target.create "Deploy" (fun _ ->
    let releaseDir = @"\bin\Plainion.CI"

    Shell.cleanDir releaseDir

    // always deploy through the zip also locally to test zip which gets uploaded to github then
    let zip = PZip.GetReleaseFile()

    Zip.unzip releaseDir zip
)

Target.create "Publish" (fun _ ->
    let zip = PZip.GetReleaseFile()

    PGitHub.Release [ zip ]

    // publish a dummy nuget package for testing
    //PNuGet.Publish (projectRoot </> "pkg")
)

Target.runOrDefault ""
