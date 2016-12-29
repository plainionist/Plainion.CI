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

    !! ( outputPath </> "TestData" )
    |> DeleteDirs

    !! ( outputPath </> "Plainion.CI.Redist.*" )
    |> DeleteFiles

    // create a dummy nuget package for testing
//    [
//        ("Plainion.CI*", Some "lib", None)
//    ]
//    |> CreateNuGetPackage (projectRoot </> "build" </> "Dummy.nuspec") (projectRoot </> "pkg")
)

Target "DeployPackage" (fun _ ->
    let releaseDir = @"\bin\Plainion.CI"

    CleanDir releaseDir

    CopyRecursive outputPath releaseDir true |> ignore

    // publish a dummy nuget package for testing
//    PublishNuGetPackage (projectRoot </> "pkg")
)

RunTarget()
