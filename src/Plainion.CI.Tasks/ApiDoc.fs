module Plainion.CI.Tasks.PApiDoc

open System.IO
open Plainion.CI
open Plainion.CI.Tasks
open Fake.Core
open Fake.IO
open Fake.IO.FileSystemOperators
open PMsBuild

let Generate (buildDefinition:BuildDefinition) projectRoot outputPath =
    if File.Exists buildDefinition.ApiDocGenExecutable |> not then
        failwithf "!! ApiDocGenExecutable not found: %s !!" buildDefinition.ApiDocGenExecutable

    let genApiDoc (project:VsProject) =
        let assemblyFile = outputPath </> project.Assembly
        if PNUnit.API.GetTestAssemblyIncludes buildDefinition.TestAssemblyPattern outputPath |> Seq.exists ((=) assemblyFile) then
            Trace.trace (sprintf "Ignoring test assembly: %s" project.Assembly)
            0
        else
            let args = 
                buildDefinition.ApiDocGenArguments
                |> replace "%1"  assemblyFile
                |> replace "%2" (Path.GetDirectoryName(project.Location))

            printfn "Running %s with %s" buildDefinition.ApiDocGenExecutable args

            Process.shellExec { Program = buildDefinition.ApiDocGenExecutable
                                Args = []
                                WorkingDir =  projectRoot
                                CommandLine = args }
        
    let ret = 
        buildDefinition.GetSolutionPath()
        |> PMsBuild.GetProjects
        |> Seq.map genApiDoc
        |> Seq.forall(fun x -> x = 0)

    match ret with
    | true -> ()
    | false -> failwith "ApiDoc generation failed"
