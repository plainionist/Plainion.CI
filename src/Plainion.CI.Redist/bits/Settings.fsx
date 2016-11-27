#if INTERACTIVE
#r "../../../bin/Debug/FAKE/FakeLib.dll"
#r "../../../bin/Debug/Plainion.CI.Core.dll"
#r "../../../bin/Debug/Plainion.Core.dll"
#else
#r "../FAKE/FakeLib.dll"
#r "../Plainion.CI.Core.dll"
#r "../Plainion.Core.dll"
#endif

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

let projectRoot = buildDefinition.RepositoryRoot
let outputPath = buildDefinition.GetOutputPath()

let projectName = Path.GetFileNameWithoutExtension(buildDefinition.GetSolutionPath())
let releaseNotesFile = projectRoot </> "ChangeLog.md"
