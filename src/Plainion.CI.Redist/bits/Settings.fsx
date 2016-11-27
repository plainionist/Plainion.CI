#I @"../../../bin/Debug"
#r "FAKE/FakeLib.dll"
#r "Plainion.CI.Core.dll"

open Fake
open Plainion.CI
open System.IO

let getProperty name =
   match getBuildParamOrDefault name null with
   | null -> 
        match environVarOrNone name with
        | Some x -> x
        | None -> failwith "Property not found: " + name
   | x -> x

let getPropertyAndTrace name =
    let value = getProperty name
    name + "=" + value |> trace 
    value

/// get get environment variable given by Plainion.CI engine
let (!%) = getProperty

let toolsHome = getProperty "ToolsHome"

let buildDefinition = BuildDefinitionSerializer.TryDeserialize( !%"BuildDefinitionFile" )
let buildRequest = BuildRequestSerializer.Deserialize()

let outputPath = buildDefinition.GetOutputPath()
let projectRoot = buildDefinition.RepositoryRoot

let projectName = Path.GetFileNameWithoutExtension(buildDefinition.GetSolutionPath())
let releaseNotesFile = projectRoot </> "ChangeLog.md"
