#r "/bin/Plainion.CI/FAKE/FakeLib.dll"
#load "/bin/Plainion.CI/bits/Settings.fsx"

open Fake

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

    DeleteDir releaseDir

    CopyRecursive outputPath releaseDir true |> ignore
)