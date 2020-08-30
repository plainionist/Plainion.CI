module Plainion.CI.Tasks.PApiDoc

open System.IO
open Plainion.CI
open Plainion.CI.Tasks
open Fake.Core
open Fake.IO
open Fake.IO.FileSystemOperators

let GenerateApiDoc (getAssemblyProjectMap:GetAssemblyProjectMap) (buildDefinition:BuildDefinition) projectRoot outputPath =
    if File.Exists buildDefinition.ApiDocGenExecutable |> not then
        failwithf "!! ApiDocGenExecutable not found: %s !!" buildDefinition.ApiDocGenExecutable

    let assemblyProjectMap = getAssemblyProjectMap()

    let genApiDoc assembly =
        let assemblyFile = outputPath </> assembly
        if PNUnit.getTestAssemblyIncludes buildDefinition outputPath |> Seq.exists ((=) assemblyFile) then
            Trace.trace (sprintf "Ignoring test assembly: %s" assembly)
            0
        else
            let args = 
                buildDefinition.ApiDocGenArguments
                |> replace "%1"  assemblyFile
                |> replace "%2" (Path.GetDirectoryName(assemblyProjectMap.[assembly]))

            printfn "Running %s with %s" buildDefinition.ApiDocGenExecutable args

            Process.shellExec { Program = buildDefinition.ApiDocGenExecutable
                                Args = []
                                WorkingDir =  projectRoot
                                CommandLine = args }
        
    let ret = 
        assemblyProjectMap.Keys
        |> Seq.map genApiDoc
        |> Seq.forall(fun x -> x = 0)

    match ret with
    | true -> ()
    | false -> failwith "ApiDoc generation failed"
