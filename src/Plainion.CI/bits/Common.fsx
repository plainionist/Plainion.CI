#I "../../../bin/Debug/FAKE"
#r "FakeLib.dll"
open Fake
open System.IO

let outputPath = getBuildParam "OutputPath" 
let projectRoot = getBuildParam "ProjectRoot" 

Target "Default" (fun _ ->
    trace "This script does not have default target. Explicitly choose one!"
)

Target "Clean" (fun _ ->
    CleanDir outputPath
)

Target "RestoreNugetPackages" (fun _ ->
    getBuildParam "SolutionFile" 
    |> RestoreMSSolutionPackages (fun p ->
         { p with
             OutputPath = Path.Combine( projectRoot, "packages" )
             Retries = 2 })
)

Target "RunNUnitTests" (fun _ ->
    !! ( outputPath + "/" + getBuildParam "TestAssemblyPattern" )
    |> NUnit (fun p -> 
        { p with
            ToolPath = getBuildParam "NUnitPath"
            DisableShadowCopy = true })
)


RunTargetOrDefault "Default"

