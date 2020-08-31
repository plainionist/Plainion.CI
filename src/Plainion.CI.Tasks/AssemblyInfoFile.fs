module Plainion.CI.Tasks.PAssemblyInfoFile

open System
open System.IO
open Plainion.CI.Tasks
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.DotNet

let Generate (getChangeLog:GetChangeLog) projectRoot projectName =
    getChangeLog()
    |> Option.iter (fun release ->
        let getAssemblyInfoAttributes vsProjName =
            [ AssemblyInfo.Title (vsProjName)
              AssemblyInfo.Product projectName
              AssemblyInfo.Description projectName
              AssemblyInfo.Copyright (sprintf "Copyright @ %i" DateTime.UtcNow.Year)
              AssemblyInfo.Version release.AssemblyVersion
              AssemblyInfo.FileVersion release.AssemblyVersion ]

        let getProjectDetails (projectPath:string) =
            let projectName = Path.GetFileNameWithoutExtension(projectPath)
            ( projectPath,
                projectName,
                Path.GetDirectoryName(projectPath),
                (getAssemblyInfoAttributes projectName)
            )

        let (|Fsproj|Csproj|) (projFileName:string) =
            match projFileName with
            | f when f.EndsWith("fsproj", StringComparison.OrdinalIgnoreCase) -> Fsproj
            | f when f.EndsWith("csproj", StringComparison.OrdinalIgnoreCase) -> Csproj
            | _  -> failwith (sprintf "Project file %s not supported. Unknown project type." projFileName)

        let isDotNetCore projectFile =
            projectFile
            |> File.ReadAllLines
            |> Seq.find(contains ("<Project"))
            |> contains ("Sdk=")

        let verifyAssemblyInfoIsIncludedInProject projectFile assemblyInfoFile =
            if projectFile |> isDotNetCore then
                ()
            else
                let assemblyInfoIsIncluded =
                    projectFile
                    |> File.ReadAllLines
                    |> Seq.exists (contains assemblyInfoFile)
                if assemblyInfoIsIncluded |> not then 
                    failwithf "AssemblyInfo file NOT included in project %s" projectFile

        !! ( projectRoot </> "src/**/*.??proj" )
        |> Seq.map getProjectDetails
        |> Seq.iter (fun (projFileName, _, folderName, attributes) ->
            match projFileName with
            | Fsproj -> let assemblyInfo = folderName </> "AssemblyInfo.fs"
                        AssemblyInfoFile.createFSharp assemblyInfo attributes
                        verifyAssemblyInfoIsIncludedInProject projFileName "AssemblyInfo.fs"
            | Csproj -> let assemblyInfo = folderName </> "Properties" </> "AssemblyInfo.cs"
                        AssemblyInfoFile.createCSharp assemblyInfo attributes
                        verifyAssemblyInfoIsIncludedInProject projFileName "AssemblyInfo.cs"
            )
        )