// load dependencies from source folder to allow bootstrapping
#r "../bin/Debug/FAKE/FakeLib.dll"
#load "../bin/Debug/bits/PlainionCI.fsx"

open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open PlainionCI

Fake.Core.Target.create "CreatePackage" (fun _ ->
    !! ( outputPath </> "*.*Tests.*" )
    ++ ( outputPath </> "*nunit*" )
    ++ ( outputPath </> "TestResult.xml" )
    |> Fake.IO.File.deleteAll

    !! ( outputPath </> "Plainion.CI.Redist.*" )
    ++ ( outputPath </> "**/*.pdb" )
    |> Fake.IO.File.deleteAll

    PZip.PackRelease()

    // create a dummy nuget package for testing
//    [
//        ("Plainion.CI*", Some "lib", None)
//    ]
//    |> PNuGet.Pack (projectRoot </> "build" </> "Dummy.nuspec") (projectRoot </> "pkg")
)

Fake.Core.Target.create "Deploy" (fun _ ->
    let releaseDir = @"\bin\Plainion.CI"

    Fake.IO.Shell.cleanDir releaseDir

    // always deploy through the zip also locally to test zip which gets uploaded to github then
    let zip = PZip.GetReleaseFile()

    Fake.IO.Zip.unzip releaseDir zip
)

Fake.Core.Target.create "Publish" (fun _ ->
    let zip = PZip.GetReleaseFile()

    PGitHub.Release [ zip ]

    // publish a dummy nuget package for testing
    //PNuGet.Publish (projectRoot </> "pkg")
)

Fake.Core.Target.runOrDefault ""
