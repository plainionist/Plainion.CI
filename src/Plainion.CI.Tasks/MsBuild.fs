module Plainion.CI.Tasks.PMsBuild

open System
open System.IO
open System.Xml.Linq
open Plainion.CI
open Fake.DotNet

let private xn n = XName.Get(n,"http://schemas.microsoft.com/developer/msbuild/2003")

//let msBuildExe = Fake.DotNet.MSBuildParams.Create().ToolPath
let msBuildExe = FromFake.MsBuild.msBuildExe

/// Retruns all project files referenced by the given solution
let GetProjectFiles (solution:string) =
    let solutionDir = Path.GetDirectoryName(solution)

    // Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Plainion.CI", "src\Plainion.CI\Plainion.CI.csproj", "{E81B5CDC-72D9-4DEB-AF55-9BA7409C7CBF}"
    File.ReadLines(solution)
    |> Seq.filter (fun line -> line.StartsWith("Project"))
    |> Seq.map (fun line -> line.Split([|','|]).[1])
    |> Seq.map(fun file -> file.Trim().Trim('"'))
    |> Seq.map(fun file -> Path.Combine(solutionDir, file))
    |> Seq.filter File.Exists
    |> List.ofSeq 

type VsProject = { Location : string
                   Assembly : string }

/// Loads the visual studio project from given location
let LoadProject (projectFile:string) =
    let root = XElement.Load(projectFile)

    let allProperties = 
        root.Elements(xn "PropertyGroup") 
        |> Seq.collect(fun e -> e.Elements())   
        |> List.ofSeq

    let assembly = 
        allProperties 
        |> Seq.tryFind(fun e -> e.Name = xn "AssemblyName")
        |> Option.map(fun e -> e.Value)
        // in .Net Core per default the project file name equals assembly name
        |> Option.defaultValue (projectFile |> Path.GetFileNameWithoutExtension)

    let outputType = 
        allProperties 
        |> Seq.tryFind(fun e -> e.Name = xn "OutputType")
        |> Option.map(fun e -> e.Value)
        |> Option.defaultValue "dll" // TODO: detection does not work for .Net Core exe

    let assemblyExtension = if outputType = "WinExe" || outputType = "Exe" then "exe" else "dll"

    { Location = projectFile
      Assembly = (sprintf "%s.%s" assembly assemblyExtension) }

/// Returns Package References
let GetPackageReferences(projectFile:string) =
    let root = XElement.Load(projectFile)

    root.Elements(XName.Get("ItemGroup"))
    |> Seq.collect(fun e -> e.Elements())   
    |> Seq.filter(fun e -> e.Name = XName.Get("PackageReference"))
    |> Seq.map(fun e -> e.Attribute(XName.Get("Include")).Value, e.Attribute(XName.Get("Version")).Value)
    |> List.ofSeq

let getAssemblyProjectMap (buildDefinition:BuildDefinition) =
    GetProjectFiles(buildDefinition.GetSolutionPath())
    |> Seq.map LoadProject
    |> Seq.map(fun proj -> proj.Assembly, proj.Location)
    |> dict

let Build (buildDefinition:BuildDefinition) outputPath =
    let setParams (defaults:MSBuildParams) =
        { defaults with
            ToolPath = msBuildExe
            Properties = [ "OutputPath", outputPath
                           "Configuration", buildDefinition.Configuration
                           "Platform", buildDefinition.Platform ] }

    MSBuild.build setParams (buildDefinition.GetSolutionPath())
