module Plainion.CI.Tasks.PNUnit

open System
open System.IO
open Plainion.CI
open Fake.Core
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.DotNet.Testing

// API provided to write custom tasks
module API =
    let GetTestAssemblyIncludes testAssemblyPattern outputPath =     
        if testAssemblyPattern |> String.IsNullOrEmpty then
            failwith "!! NO TEST ASSEMBLY PATTERN PROVIDED !!"

        let testAssemblyPatterns = 
            testAssemblyPattern.Split(';')
            |> Seq.map ((</>) outputPath)

        testAssemblyPatterns
        |> Seq.skip 1
        |> Seq.fold (++) (!! (testAssemblyPatterns |> Seq.head))

type RunTestsRequest = {
    TestRunnerExecutable : string
    TestAssemblyPattern : string
    ProjectRoot : string
    OutputPath : string
} with 
    static member Create (def: BuildDefinition) =
        {
            TestRunnerExecutable = def.TestRunnerExecutable
            TestAssemblyPattern = def.TestAssemblyPattern
            ProjectRoot = def.RepositoryRoot
            OutputPath = def.GetOutputPath()
        }

let RunTests request = 
    let toolPath = Path.GetDirectoryName( request.TestRunnerExecutable )

    if File.Exists ( toolPath </> "nunit-console.exe" ) then
        API.GetTestAssemblyIncludes request.TestAssemblyPattern request.OutputPath
        // "parallel" version does not show test output
        |> NUnit.Sequential.run (fun p -> 
            { p with
                ToolPath = toolPath
                DisableShadowCopy = true })
    elif File.Exists ( toolPath </> "nunit3-console.exe" ) then
        API.GetTestAssemblyIncludes request.TestAssemblyPattern request.OutputPath
        |> NUnit3.run (fun p -> 
            { p with
                ToolPath = toolPath </> "nunit3-console.exe"
                ShadowCopy = false })
    else // e.g. "dotnet test"
        let ret = 
            Process.shellExec { Program = request.TestRunnerExecutable
                                Args = []
                                WorkingDir =  request.ProjectRoot
                                CommandLine = request.TestAssemblyPattern }
        if ret <> 0 then
            failwith "Test execution failed"
