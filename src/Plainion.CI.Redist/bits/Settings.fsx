#I "../../../bin/Debug/FAKE"
#r "FakeLib.dll"
open Fake

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
let (!%) = getPropertyAndTrace

let toolsHome = getProperty "ToolsHome"
let outputPath = !%"OutputPath"
let projectRoot = !%"ProjectRoot" 

