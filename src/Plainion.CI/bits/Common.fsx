#I "../../../bin/Debug/FAKE"
#r "FakeLib.dll"
open Fake

let outputPath = getBuildParam "OutputPath" 

Target "Default" (fun _ ->
    trace "This script does not have default target. Explicitly choose one!"
)

Target "Clean" (fun _ ->
    CleanDir outputPath
)

RunTargetOrDefault "Default"

