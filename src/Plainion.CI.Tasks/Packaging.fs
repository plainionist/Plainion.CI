module Plainion.CI.Tasks.PPackaging

open System
open System.IO
open Plainion.CI
open Fake.Core
open Fake.IO
open Fake.IO.FileSystemOperators

let runScript projectRoot outputPath (script:string) (args:string) =
    let ret = 
        if script.EndsWith(".fsx", StringComparison.OrdinalIgnoreCase) then
            { Program = @"Plainion.CI.BuildHost.exe"
              Args = []
              WorkingDir = projectRoot
              CommandLine = (sprintf "%s %s" script args) }
            |> Process.shellExec 
        elif script.EndsWith(".msbuild", StringComparison.OrdinalIgnoreCase) || script.EndsWith(".targets", StringComparison.OrdinalIgnoreCase) then
            { Program = @"C:\Program Files (x86)\MSBuild\12.0\Bin\MSBuild.exe"
              Args = []
              WorkingDir = projectRoot
              CommandLine = (sprintf "/p:OutputPath=%s %s %s" outputPath args script) }
            |> Process.shellExec 
        else
            failwithf "Unknown script type: %s" script

    match ret with
    | 0 -> ()
    | _-> failwithf "Execution of script %s failed with %i" script ret

let private getPackagingScript (buildDefinition:BuildDefinition) projectRoot =
    let script = projectRoot </> buildDefinition.PackagingScript
    if script |> File.Exists |> not then
        failwithf "Packaging script does not exist: %s" buildDefinition.PackagingScript
    script

let CreatePackage buildDefinition projectRoot outputPath = 
    let script = getPackagingScript buildDefinition projectRoot    
    runScript projectRoot outputPath script buildDefinition.CreatePackageArguments

let DeployPackage buildDefinition projectRoot outputPath = 
    let script = getPackagingScript buildDefinition projectRoot    
    runScript projectRoot outputPath script buildDefinition.DeployPackageArguments

let PublishPackage buildDefinition projectRoot outputPath = 
    let script = getPackagingScript buildDefinition projectRoot    
    runScript projectRoot outputPath script buildDefinition.PublishPackageArguments
