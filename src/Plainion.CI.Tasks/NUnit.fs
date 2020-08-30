module Plainion.CI.Tasks.PNUnit

open System
open System.IO
open Plainion.CI
open Fake.Core
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.DotNet.Testing

let getTestAssemblyIncludes (buildDefinition:BuildDefinition) outputPath =     
    if buildDefinition.TestAssemblyPattern |> String.IsNullOrEmpty then
        failwith "!! NO TEST ASSEMBLY PATTERN PROVIDED !!"

    let testAssemblyPatterns = 
        buildDefinition.TestAssemblyPattern.Split(';')
        |> Seq.map ((</>) outputPath)

    testAssemblyPatterns
    |> Seq.skip 1
    |> Seq.fold (++) (!! (testAssemblyPatterns |> Seq.head))

let RunTests (buildDefinition:BuildDefinition) projectRoot outputPath = 
    let toolPath = Path.GetDirectoryName( buildDefinition.TestRunnerExecutable )

    if File.Exists ( toolPath </> "nunit-console.exe" ) then
        getTestAssemblyIncludes buildDefinition outputPath
        // "parallel" version does not show test output
        |> NUnit.Sequential.run (fun p -> 
            { p with
                ToolPath = toolPath
                DisableShadowCopy = true })
    elif File.Exists ( toolPath </> "nunit3-console.exe" ) then
        getTestAssemblyIncludes buildDefinition outputPath
        |> NUnit3.run (fun p -> 
            { p with
                ToolPath = toolPath </> "nunit3-console.exe"
                ShadowCopy = false })
    else // e.g. "dotnet test"
        let ret = 
            Process.shellExec { Program = buildDefinition.TestRunnerExecutable
                                Args = []
                                WorkingDir =  projectRoot
                                CommandLine = buildDefinition.TestAssemblyPattern }
        if ret <> 0 then
            failwith "Test execution failed"
