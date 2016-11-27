#I "../../../bin/Debug/FAKE"

#load "Targets.fsx"

open Fake
open Settings

"Clean"
    ==> "RestoreNugetPackages"
    ==> "Build"
    =?> ("GenerateApiDoc", buildDefinition.GenerateAPIDoc)
    =?> ("RunNUnitTests", buildDefinition.RunTests)
    =?> ("Commit", buildDefinition.CheckIn)
    =?> ("Push", buildDefinition.Push)
    ==> "Default"

RunTargetOrDefault "Default"
