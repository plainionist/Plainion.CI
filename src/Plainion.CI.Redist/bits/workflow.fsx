#I "../../../bin/Debug/FAKE"

#load "Targets.fsx"

open Fake
open Settings

"Clean"
    ==> "RestoreNugetPackages"
    ==> "Build"
    =?> ("GenerateApiDoc", !%"Option.ApiDoc" |> toBool)
    =?> ("RunNUnitTests", !%"Option.Tests" |> toBool)
    =?> ("Commit", buildDefinition.CheckIn)
    =?> ("Push", buildDefinition.Push)
    ==> "Default"

RunTargetOrDefault "Default"
