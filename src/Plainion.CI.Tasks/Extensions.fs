module Plainion.CI.Tasks.Extensions

open System
open System.IO
open Fake.Core
open Fake.IO
open Fake.IO.FileSystemOperators

type ExecExtensionRequest = {
    ProjectRoot : string
    OutputPath : string
    ExtensionPath : string
    ExtensionArguments : string
} 

let Exec request = 
    let script = request.ProjectRoot </> request.ExtensionPath
    if script |> File.Exists |> not then
        failwithf "Extension does not exist: %s" request.ExtensionPath

    let ret = 
        if script.EndsWith(".fsx", StringComparison.OrdinalIgnoreCase) then
            {   Program = @"Plainion.CI.BuildHost.exe"
                Args = []
                WorkingDir = request.ProjectRoot
                CommandLine = (sprintf "%s %s" script request.ExtensionArguments) }
            |> Process.shellExec 
        elif script.EndsWith(".msbuild", StringComparison.OrdinalIgnoreCase) || script.EndsWith(".targets", StringComparison.OrdinalIgnoreCase) then
            {   Program = @"C:\Program Files (x86)\MSBuild\12.0\Bin\MSBuild.exe"
                Args = []
                WorkingDir = request.ProjectRoot
                CommandLine = (sprintf "/p:OutputPath=%s %s %s" request.OutputPath request.ExtensionArguments script) }
            |> Process.shellExec 
        else
            failwithf "Unknown script type: %s" script

    match ret with
    | 0 -> ()
    | _-> failwithf "Execution of script %s failed with %i" script ret
