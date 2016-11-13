#I "../../../bin/Debug/FAKE"

#load "Settings.fsx"
#r "FakeLib.dll"

open Fake
open Fake.Testing.NUnit3
open System.IO
open System.Diagnostics
open System
open Settings

let setParams defaults =
    { defaults with
        Targets = ["Build"]
        Properties = [ "OutputPath", outputPath
                       "Configuration", !%"Configuration"
                       "Platform", !%"Platform"
                     ]
    }

Target "Default" (fun _ ->
    trace "This script does not have default target. Explicitly choose one!"
)

Target "Clean" (fun _ ->
    CleanDir outputPath
)

Target "Bootstrap" (fun _ ->
    build setParams (projectRoot </> "src" </> "Plainion.CI.Redist" </> "Plainion.CI.Redist.csproj")
)

Target "Build" (fun _ ->
    build setParams (!%"SolutionFile")
)

Target "RestoreNugetPackages" (fun _ ->
    !%"SolutionFile" 
    |> RestoreMSSolutionPackages (fun p ->
         { p with
             OutputPath = projectRoot </> "packages"
             Retries = 1 })
)

Target "RunNUnitTests" (fun _ ->
    let assemblies = !! ( outputPath + "/" + !%"TestAssemblyPattern" )
    let toolPath = !%"NUnitPath"

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
        let args = (!%"ApiDocGenArguments").Replace("%1", assembly)
        shellExec { Program = !%"ApiDocGenExecutable"
                    Args = []
                    WorkingDirectory =  !%"ProjectRoot"
                    CommandLine = args}
    
    let projectName = Path.GetFileNameWithoutExtension(!%"SolutionFile")

    let assemblies = 
        !! ( outputPath + "/" + "*.dll" )
        ++ ( outputPath + "/" + "*.exe" )
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

