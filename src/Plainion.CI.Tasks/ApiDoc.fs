module Plainion.CI.Tasks.PApiDoc

open System.IO
open Plainion.CI
open Plainion.CI.Tasks
open Fake.Core
open Fake.IO
open Fake.IO.FileSystemOperators
open PMsBuild

type ApiDocRequest = {
    ProjectRoot : string
    OutputPath : string
    ApiDocGenExecutable : string
    ApiDocGenArguments : string
    TestAssemblyPattern : string
    SolutionPath : string
} with 
    static member Create (def:BuildDefinition) =
        {
            ProjectRoot = def.RepositoryRoot
            OutputPath = def.GetOutputPath()
            ApiDocGenExecutable = def.ApiDocGenExecutable
            ApiDocGenArguments = def.ApiDocGenArguments
            TestAssemblyPattern = def.TestAssemblyPattern
            SolutionPath = def.GetSolutionPath()
        }

let Generate request =
    if File.Exists request.ApiDocGenExecutable |> not then
        failwithf "!! ApiDocGenExecutable not found: %s !!" request.ApiDocGenExecutable

    let genApiDoc (project:VsProject) =
        let assemblyFile = request.OutputPath </> project.Assembly
        if PNUnit.API.GetTestAssemblyIncludes request.TestAssemblyPattern request.OutputPath |> Seq.exists ((=) assemblyFile) then
            Trace.trace (sprintf "Ignoring test assembly: %s" project.Assembly)
            0
        else
            let args = 
                request.ApiDocGenArguments
                |> replace "%1"  assemblyFile
                |> replace "%2" (Path.GetDirectoryName(project.Location))

            printfn "Running %s with %s" request.ApiDocGenExecutable args

            Process.shellExec { Program = request.ApiDocGenExecutable
                                Args = []
                                WorkingDir =  request.ProjectRoot
                                CommandLine = args }
        
    let ret = 
        request.SolutionPath
        |> PMsBuild.API.GetProjects
        |> Seq.map genApiDoc
        |> Seq.forall(fun x -> x = 0)

    match ret with
    | true -> ()
    | false -> failwith "ApiDoc generation failed"
