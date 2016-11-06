#I "../../../bin/Debug/FAKE"
#load "Settings.fsx"
#r "FakeLib.dll"
open Fake
open Fake.Testing.NUnit3
open System.IO
open System.Diagnostics
open System

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

Target "GenerateApiDoc" (fun _ ->
    let genApiDoc assembly =
        let args = (Settings.getPropertyAndTrace "ApiDocGenArguments").Replace("%1", assembly)
        shellExec { Program = Settings.getPropertyAndTrace "ApiDocGenExecutable"
                    Args = []
                    WorkingDirectory =  Settings.getPropertyAndTrace "ProjectRoot"
                    CommandLine = args}
    
    let projectName = Path.GetFileNameWithoutExtension(Settings.getPropertyAndTrace "SolutionFile")

    let assemblies = 
        !! ( Settings.outputPath + "/" + "*.dll" )
        ++ ( Settings.outputPath + "/" + "*.exe" )
        |> Seq.filter(fun f -> Path.GetFileName(f).StartsWith(projectName, StringComparison.OrdinalIgnoreCase))
        |> List.ofSeq

    printfn "Assemblies:"
    assemblies |> Seq.iter(fun x -> printfn " - %s" x)

    let ret = 
        assemblies
        |> Seq.map genApiDoc
        |> Seq.forall(fun x -> x = 0)

    match ret with
    | true -> ()
    | false -> failwith "ApiDoc generation failed"
)

RunTargetOrDefault "Default"

