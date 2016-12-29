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
)

Target "DeployPackage" (fun _ ->
    let releaseDir = @"\bin\Plainion.CI"

    CleanDir releaseDir

    CopyRecursive outputPath releaseDir true |> ignore
)

RunTarget()
