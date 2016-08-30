#I "../../../bin/Debug/FAKE"
#load "Settings.fsx"
#r "FakeLib.dll"
open Fake
open Fake.Testing.NUnit3
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
    let assemblies = !! ( Settings.outputPath + "/" + Settings.getPropertyAndTrace "TestAssemblyPattern" )
    let toolPath = Settings.getPropertyAndTrace "NUnitPath"

    if fileExists ( toolPath @@ "nunit-console.exe" ) then
        assemblies
        |> NUnitParallel (fun p -> 
            { p with
                ToolPath = toolPath
                DisableShadowCopy = true })
    else
        assemblies
        |> NUnit3 (fun p -> 
            { p with
                ToolPath = toolPath @@ "nunit3-console.exe"
                ShadowCopy = false })
)

RunTargetOrDefault "Default"

