// load dependencies from source folder to allow bootstrapping
#r "../bin/Debug/FAKE/FakeLib.dll"
#load "../bin/Debug/bits/PlainionCI.fsx"

open Fake
open PlainionCI

Target "CreatePackage" (fun _ ->
    !! ( outputPath </> "*.*Tests.*" )
    ++ ( outputPath </> "*nunit*" )
    ++ ( outputPath </> "TestResult.xml" )
    |> DeleteFiles

    !! ( outputPath </> "Plainion.CI.Redist.*" )
    ++ ( outputPath </> "**/*.pdb" )
    |> DeleteFiles

    PZip.PackRelease()

    // create a dummy nuget package for testing
    //[
    //    ("Plainion.CI*", Some "lib", None)
    //]
    //|> PNuGet.Pack (projectRoot </> "build" </> "Dummy.nuspec") (projectRoot </> "pkg")
)

Target "DeployPackage" (fun _ ->
    let releaseDir = @"\bin\Plainion.CI"

    CleanDir releaseDir

    // always deploy through the zip also locally to test zip which gets uploaded to github then
    let zip = PZip.GetReleaseFile()

    Unzip releaseDir zip

    //PGitHub.Release [ zip ]

    // publish a dummy nuget package for testing
    //PNuGet.Publish (projectRoot </> "pkg")
)

RunTarget()
