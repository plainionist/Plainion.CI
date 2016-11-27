#I "../../../bin/Debug/FAKE"
#I "../../../bin/Debug"

#load "Settings.fsx"
#r "FakeLib.dll"
#r "Plainion.Core.dll"
#r "Plainion.CI.Core.dll"
#r "Plainion.CI.Tasks.dll"

open System
open System.IO
open Fake
open Fake.Testing.NUnit3
open Fake.AssemblyInfoFile
open Fake.ReleaseNotesHelper
open Plainion.CI
open Settings

let setParams defaults =
    { defaults with
        Targets = ["Build"]
        Properties = [ "OutputPath", outputPath
                       "Configuration", buildDefinition.Configuration
                       "Platform", buildDefinition.Platform
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
    build setParams (buildDefinition.GetSolutionPath())
)

Target "RestoreNugetPackages" (fun _ ->
    buildDefinition.GetSolutionPath()
    |> RestoreMSSolutionPackages (fun p ->
         { p with
             OutputPath = projectRoot </> "packages"
             Retries = 1 })
)

Target "RunNUnitTests" (fun _ ->
    let assemblies = !! ( outputPath </> buildDefinition.TestAssemblyPattern )
    let toolPath = Path.GetDirectoryName( buildDefinition.TestRunnerExecutable )

    if fileExists ( toolPath </> "nunit-console.exe" ) then
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
                ToolPath = toolPath </> "nunit3-console.exe"
                ShadowCopy = false })
)

Target "GenerateApiDoc" (fun _ ->
    let genApiDoc assembly =
        let args = (buildDefinition.ApiDocGenArguments).Replace("%1", assembly)
        shellExec { Program = buildDefinition.ApiDocGenExecutable
                    Args = []
                    WorkingDirectory =  projectRoot
                    CommandLine = args}
    
    let assemblies = 
        !! ( outputPath </> "*.dll" )
        ++ ( outputPath </> "*.exe" )
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
    if buildRequest.CheckInComment |> String.IsNullOrEmpty then
        failwith "!! NO CHECKIN COMMENT PROVIDED !!"
    
    Plainion.CI.Tasks.Git.Commit projectRoot (buildRequest.Files |> List.ofSeq, buildRequest.CheckInComment, buildDefinition.User.Login, buildDefinition.User.EMail)
)

Target "Push" (fun _ ->
    if buildDefinition.User.Password = null then
        failwith "!! NO PASSWORD PROVIDED !!"
    
    Plainion.CI.Tasks.Git.Push projectRoot (buildDefinition.User.Login, buildDefinition.User.Password.ToUnsecureString())
)

Target "AssemblyInfo" (fun _ ->
    let release = LoadReleaseNotes releaseNotesFile
    
    let getAssemblyInfoAttributes vsProjName =
        [ Attribute.Title (vsProjName)
          Attribute.Product projectName
          Attribute.Description projectName
          Attribute.Copyright (sprintf "Copyright @ %i" DateTime.UtcNow.Year)
          Attribute.Version release.AssemblyVersion 
          Attribute.FileVersion release.AssemblyVersion ]

    let getProjectDetails projectPath =
        let projectName = Path.GetFileNameWithoutExtension(projectPath)
        ( projectPath,
          projectName,
          Path.GetDirectoryName(projectPath),
          (getAssemblyInfoAttributes projectName)
        )

    let (|Fsproj|Csproj|) (projFileName:string) =
        match projFileName with
        | f when f.EndsWith("fsproj") -> Fsproj
        | f when f.EndsWith("csproj") -> Csproj
        | _                           -> failwith (sprintf "Project file %s not supported. Unknown project type." projFileName)

    !! ( projectRoot </> "src/**/*.??proj" )
    |> Seq.map getProjectDetails
    |> Seq.iter (fun (projFileName, projectName, folderName, attributes) ->
        match projFileName with
        | Fsproj -> CreateFSharpAssemblyInfo (folderName </> "AssemblyInfo.fs") attributes
        | Csproj -> CreateCSharpAssemblyInfo ((folderName </> "Properties") </> "AssemblyInfo.cs") attributes
        )
)

