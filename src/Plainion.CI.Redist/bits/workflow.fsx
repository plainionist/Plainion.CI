#I "../../../bin/Debug/FAKE"

#load "Targets.fsx"

open Fake
open Settings
open Targets

"Clean"
    ==> "RestoreNugetPackages"
    ==> "Build"
    =?> ("GenerateApiDoc", !%"Option.ApiDoc" |> toBool)
    =?> ("RunNUnitTests", !%"Option.Tests" |> toBool)
    ==> "Default"

RunTargetOrDefault "Default"
