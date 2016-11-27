#I "../../../bin/Debug/FAKE"
#I "../../../bin/Debug"

#load "Settings.fsx"
#r "FakeLib.dll"
#r "Plainion.CI.Tasks.dll"

open Fake
open Fake.Testing.NUnit3
open System.IO
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
    trace "--- Plainion.CI - DONE ---"
)

Target "Nop" (fun _ -> () )

Target "Clean" (fun _ ->
    CleanDir outputPath
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
        // "parallel" version does not show test output
        |> NUnit (fun p -> 
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

Target "Commit" (fun _ ->
//    if buildRequest.CheckInComment |> String.IsNullOrEmpty then
//        failwith "!! NO CHECKIN COMMENT PROVIDED !!"
//    
//    Plainion.CI.Tasks.Git.Commit projectRoot (buildRequest.Files, buildRequest.CheckInComment, buildDefinition.User.Login, buildDefinition.User.EMail)
)

