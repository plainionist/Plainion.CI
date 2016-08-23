#I "../../../bin/Debug/FAKE"
#r "FakeLib.dll"
open Fake
open System.IO

let outputPath = getBuildParam "OutputPath" 
let projectRoot = getBuildParam "ProjectRoot" 
let solutionFile = getBuildParam "SolutionFile" 

Target "Default" (fun _ ->
    trace "This script does not have default target. Explicitly choose one!"
)

Target "Clean" (fun _ ->
    CleanDir outputPath
)

Target "RestoreNugetPackages" (fun _ ->
    solutionFile
    |> RestoreMSSolutionPackages (fun p ->
         { p with
             OutputPath = Path.Combine( projectRoot, "packages" )
             Retries = 2 })
)

RunTargetOrDefault "Default"

