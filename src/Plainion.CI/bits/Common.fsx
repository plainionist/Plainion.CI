#I "../../../bin/Debug/FAKE"
#r "FakeLib.dll"
open Fake
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

let outputPath = getPropertyAndTrace "OutputPath"
let projectRoot = getPropertyAndTrace "ProjectRoot" 

Target "Default" (fun _ ->
    trace "This script does not have default target. Explicitly choose one!"
)

Target "Clean" (fun _ ->
    CleanDir outputPath
)

Target "RestoreNugetPackages" (fun _ ->
    getPropertyAndTrace "SolutionFile" 
    |> RestoreMSSolutionPackages (fun p ->
         { p with
             OutputPath = Path.Combine( projectRoot, "packages" )
             Retries = 2 })
)

Target "RunNUnitTests" (fun _ ->
    !! ( outputPath + "/" + getPropertyAndTrace "TestAssemblyPattern" )
    |> NUnitParallel (fun p -> 
        { p with
            ToolPath = getPropertyAndTrace "NUnitPath"
            DisableShadowCopy = true })
)

#load "ApiDoc.fsx"

Target "GenerateApiDoc" (fun _ ->
    ApiDoc.generateApiDoc()
)


RunTargetOrDefault "Default"

