#I "../../../bin/Debug/FAKE"
#load "Settings.fsx"
#r "FakeLib.dll"
open Fake
open System.IO

Target "Default" (fun _ ->
    trace "This script does not have default target. Explicitly choose one!"
)

Target "Clean" (fun _ ->
    CleanDir Settings.outputPath
)

Target "RestoreNugetPackages" (fun _ ->
    Settings.getPropertyAndTrace "SolutionFile" 
    |> RestoreMSSolutionPackages (fun p ->
         { p with
             OutputPath = Path.Combine( Settings.projectRoot, "packages" )
             Retries = 1 })
)

Target "RunNUnitTests" (fun _ ->
    !! ( Settings.outputPath + "/" + Settings.getPropertyAndTrace "TestAssemblyPattern" )
    |> NUnitParallel (fun p -> 
        { p with
            ToolPath = Settings.getPropertyAndTrace "NUnitPath"
            DisableShadowCopy = true })
)

RunTargetOrDefault "Default"

