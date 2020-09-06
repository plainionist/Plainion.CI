module Plainion.CI.Tasks.PMsBuild

open System.IO
open System.Xml.Linq
open Plainion.CI
open Fake.DotNet

type PackageReference = {
    Name : string
    Version : string
}

type VsProject = { 
    Location : string
    Assembly : string
    PackageReferences : PackageReference list
}

[<AutoOpen>]
module private Impl =
    let xn n = XName.Get(n,"http://schemas.microsoft.com/developer/msbuild/2003")

    //let msBuildExe = Fake.DotNet.MSBuildParams.Create().ToolPath
    let msBuildExe = FromFake.MsBuild.msBuildExe

    let getProjectFiles (solution:string) =
        let solutionDir = Path.GetDirectoryName(solution)

        // Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Plainion.CI", "src\Plainion.CI\Plainion.CI.csproj", "{E81B5CDC-72D9-4DEB-AF55-9BA7409C7CBF}"
        File.ReadLines(solution)
        |> Seq.filter (fun line -> line.StartsWith("Project"))
        |> Seq.map (fun line -> line.Split([|','|]).[1])
        |> Seq.map(fun file -> file.Trim().Trim('"'))
        |> Seq.map(fun file -> Path.Combine(solutionDir, file))
        |> Seq.filter File.Exists
        |> List.ofSeq 

    let getPackageReferences (doc:XElement) =
        doc.Elements(XName.Get("ItemGroup"))
        |> Seq.collect(fun e -> e.Elements())   
        |> Seq.filter(fun e -> e.Name = XName.Get("PackageReference"))
        |> Seq.map(fun e -> {
            Name = e.Attribute(XName.Get("Include")).Value
            Version = e.Attribute(XName.Get("Version")).Value 
            })
        |> List.ofSeq

    let loadProject (projectFile:string) =
        let doc = XElement.Load(projectFile)

        let allProperties = 
            doc.Elements(xn "PropertyGroup") 
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
          Assembly = (sprintf "%s.%s" assembly assemblyExtension)
          PackageReferences = doc |> getPackageReferences }

module API =          
    let GetProjects =
        let mutable projects : VsProject list = []
        fun (solution:string) ->
            if projects.IsEmpty then
                projects <-  
                    solution
                    |> getProjectFiles
                    |> List.map loadProject
            projects

type BuildRequest = {
    SolutionPath : string
    OutputPath : string
    Configuration : string
    Platform : string
} with
    static member Create (def:BuildDefinition) =
        {
            SolutionPath = def.GetSolutionPath()
            OutputPath = def.GetOutputPath()
            Configuration = def.Configuration
            Platform = def.Platform
        }

let Build request =
    let setParams (defaults:MSBuildParams) =
        { defaults with
            ToolPath = msBuildExe
            Properties = [ "OutputPath", request.OutputPath
                           "Configuration", request.Configuration
                           "Platform", request.Platform ] }

    MSBuild.build setParams request.SolutionPath
