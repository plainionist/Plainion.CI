#if FAKE
#r "../FAKE/FakeLib.dll"
#r "../Plainion.Core.dll"
#r "../Plainion.CI.Core.dll"
#r "../Plainion.CI.Tasks.dll"
#else
#r "../../../bin/Debug/FAKE/FakeLib.dll"
#r "../../../bin/Debug/Plainion.Core.dll"
#r "../../../bin/Debug/Plainion.CI.Core.dll"
#r "../../../bin/Debug/Plainion.CI.Tasks.dll"
#endif

open System.IO
open Plainion.CI
open Plainion.CI.Tasks
open Fake.Core
open Fake.IO
open Fake.Core.TargetOperators

Target.create "All" Targets.All

Target.create "Clean" Targets.Clean

Target.create "Build" Targets.Build

Target.create "RunTests" Targets.RunTests

Target.create "GenerateApiDoc" Targets.GenerateApiDoc

Target.create "Commit" Targets.Commit

Target.create "Push" Targets.Push

Target.create "AssemblyInfo" Targets.AssemblyInfo

Target.create "CreatePackage" Targets.CreatePackage

Target.create "DeployPackage" Targets.DeployPackage

Target.create "PublishPackage" Targets.PublishPackage

"Clean"
    =?> ("AssemblyInfo", changeLogFile |> File.Exists)
    ==> "Build"
    =?> ("GenerateApiDoc", buildDefinition.GenerateAPIDoc)
    =?> ("RunTests", buildDefinition.RunTests)
    =?> ("Commit", buildDefinition.CheckIn)
    =?> ("Push", buildDefinition.Push)
    =?> ("CreatePackage", buildDefinition.CreatePackage)
    =?> ("DeployPackage", buildDefinition.DeployPackage)
    =?> ("PublishPackage", buildDefinition.PublishPackage)
    ==> "All"

Target.runOrDefault ""
