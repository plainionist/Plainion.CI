// TODO: temporary solution to migrate to FAKE 5 (.net core)
namespace Plainion.CI

open System
open System.IO
open Plainion.CI
open Plainion.CI.Tasks
open Fake.Core
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.DotNet
open Fake.DotNet.Testing
open Fake.Runtime
open Fake.Runtime.Trace
open System.Reflection

[<AutoOpen>]
module Common =
    let getProperty name =
        match name |> Environment.environVarOrNone with
        | Some x -> x
        | None -> failwithf "Property not found: %s" name

    let getPropertyAndTrace name =
        let value = getProperty name
        name + "=" + value |> Trace.trace 
        value

    /// get environment variable given by Plainion.CI engine
    let (!%) = getProperty

    let toolsHome = getProperty "ToolsHome"

    let buildDefinition = BuildDefinitionSerializer.TryDeserialize( !%"BuildDefinitionFile" )
    let buildRequest = BuildRequestSerializer.Deserialize()

    let projectRoot = buildDefinition.RepositoryRoot
    let outputPath = buildDefinition.GetOutputPath()

    let projectName = Path.GetFileNameWithoutExtension(buildDefinition.GetSolutionPath())
    let changeLogFile = projectRoot </> "ChangeLog.md"

    let private changeLog = lazy ( match File.exists changeLogFile with
                                   | true -> changeLogFile |> ReleaseNotes.load |> Some
                                   | false -> None
                                 )

    /// Returns the parsed ChangeLog.md if exists
    let getChangeLog () = 
        match changeLog.Value with
        | Some cl -> cl
        | None -> failwith "No ChangeLog.md found in project root"


    let private assemblyProjects = lazy (   let projects = PMsBuild.GetProjectFiles(buildDefinition.GetSolutionPath())
                                            projects
                                            |> Seq.map PMsBuild.LoadProject
                                            |> Seq.map(fun proj -> proj.Assembly, proj.Location)
                                            |> dict
                                        )

    /// Returns a dictionary mapping assembly names to their project files based on the project solution
    let getAssemblyProjectMap() =
        assemblyProjects.Value

    module PZip =
        let private getReleaseName() =
            let release = getChangeLog()
            sprintf "%s-%s" projectName release.NugetVersion

        let GetReleaseFile () =
            outputPath </> ".." </> (sprintf "%s.zip" (getReleaseName()))

        /// Creates a zip from all content of the OutputPath with current version backed in
        let PackRelease() = 
            let zip = GetReleaseFile()
            let releaseName = getReleaseName()

            !! ( outputPath </> "**/*.*" )
            |> Zip.zip outputPath zip

module PNuGet =
    open Fake.DotNet.NuGet

    /// Creates a NuGet package with the given files and NuSpec at the packageOut folder.
    /// Version is taken from changelog.md
    let Pack nuspec packageOut files =
        let release = getChangeLog()
        
        Directory.create packageOut
        Shell.cleanDir packageOut

        let assemblies = 
            files 
            |> Seq.map(fun (source,_,_) -> source)
            |> Seq.collect(fun pattern -> !! (outputPath </> pattern))
            |> Seq.map Path.GetFileName
            |> List.ofSeq

        assemblies
        |> Seq.iter( fun a -> Trace.trace (sprintf "Adding file %s to package" a))

        let dependencies =
            let getDependencies (projectFile:string) =
                let packagesConfig = projectFile |> Path.GetDirectoryName </> "packages.config"

                if packagesConfig |> File.exists then
                    packagesConfig 
                    |> Fake.DotNet.NuGet.NuGet.getDependencies
                    |> List.map(fun d -> d.Id,d.Version.AsString)
                else
                    //     <PackageReference Include="System.ComponentModel" Version="4.3.0" />
                    projectFile 
                    |> PMsBuild.GetPackageReferences

            getAssemblyProjectMap()
            |> Seq.filter(fun e -> assemblies |> List.exists ((=)e.Key))
            |> Seq.collect(fun e -> e.Value |> getDependencies)
            |> Seq.distinct
            |> List.ofSeq

        dependencies
        |> Seq.iter( fun d -> Trace.trace (sprintf "Package dependency detected: %A" d))

        nuspec 
        |>  NuGet.NuGet (fun p ->  {p with OutputPath = packageOut
                                           WorkingDir = outputPath
                                           Project = projectName
                                           Dependencies = dependencies 
                                           Version = release.AssemblyVersion
                                           ReleaseNotes = release.Notes 
                                                          |> String.concat Environment.NewLine
                                           Files = files }) 
    
    /// Publishes the NuGet package specified by packageOut, projectName and current version of ChangeLog.md
    /// to NuGet (https://www.nuget.org/api/v2/package)              
    let PublishPackage packageName packageOut =
        let release = getChangeLog()

        NuGet.NuGetPublish (fun p -> {p with OutputPath = packageOut
                                             WorkingDir = projectRoot
                                             Project = packageName
                                             Version = release.AssemblyVersion
                                             PublishUrl = "https://www.nuget.org/api/v2/package"
                                             Publish = true }) 

    /// Publishes the NuGet package specified by packageOut, projectName and current version of ChangeLog.md
    /// to NuGet (https://www.nuget.org/api/v2/package)              
    let Publish packageOut =
        PublishPackage projectName packageOut

module PGitHub =
    open Plainion.CI.Tasks
    open Fake.Tools.Git

    /// Publishes a new release to GitHub with the current version of ChangeLog.md and
    /// the given files
    let Release files =
        if buildDefinition.User.Password = null then
            failwith "!! NO PASSWORD PROVIDED !!"
    
        let release = getChangeLog()

        let user = buildDefinition.User.Login
        let pwd = buildDefinition.User.Password.ToUnsecureString()

        try
            Branches.deleteTag "" release.NugetVersion
        with | _ -> ()
        
        Branches.tag "" release.NugetVersion
        PGit.Push projectRoot (user, pwd)
    
        // release on GitHub
        
        let releaseNotes =  release.Notes 
                            |> List.ofSeq

        PGitHub.createDraft user pwd projectName release.NugetVersion (release.SemVer.PreRelease <> None) releaseNotes 
        |> PGitHub.uploadFiles files  
        |> PGitHub.releaseDraft
        |> Async.RunSynchronously


module Targets =
    let All = (fun _ ->
        Trace.trace "--- Plainion.CI - DONE ---"
    )

    let Clean = (fun _ ->
        Shell.cleanDir outputPath
    )

    let Build = (fun _ ->

        let setParams (defaults:MSBuildParams) =
            { defaults with
                ToolPath = PMsBuild.msBuildExe
                Properties = [ "OutputPath", outputPath
                               "Configuration", buildDefinition.Configuration
                               "Platform", buildDefinition.Platform ] }

        MSBuild.build setParams (buildDefinition.GetSolutionPath())
    )

    let private testAssemblyIncludes () =     
        if buildDefinition.TestAssemblyPattern |> String.IsNullOrEmpty then
            failwith "!! NO TEST ASSEMBLY PATTERN PROVIDED !!"

        let testAssemblyPatterns = 
            buildDefinition.TestAssemblyPattern.Split(';')
            |> Seq.map ((</>) outputPath)

        testAssemblyPatterns
        |> Seq.skip 1
        |> Seq.fold (++) (!! (testAssemblyPatterns |> Seq.head))

    let RunTests = (fun _ ->
        let toolPath = Path.GetDirectoryName( buildDefinition.TestRunnerExecutable )

        if File.Exists ( toolPath </> "nunit-console.exe" ) then
            testAssemblyIncludes()
            // "parallel" version does not show test output
            |> NUnit.Sequential.run (fun p -> 
                { p with
                    ToolPath = toolPath
                    DisableShadowCopy = true })
        elif File.Exists ( toolPath </> "nunit3-console.exe" ) then
            testAssemblyIncludes()
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
    )

    let GenerateApiDoc = (fun _ ->
        if File.Exists buildDefinition.ApiDocGenExecutable |> not then
            failwithf "!! ApiDocGenExecutable not found: %s !!" buildDefinition.ApiDocGenExecutable

        let assemblyProjectMap = getAssemblyProjectMap()

        let genApiDoc assembly =
            let assemblyFile = outputPath </> assembly
            if testAssemblyIncludes() |> Seq.exists ((=) assemblyFile) then
                Trace.trace (sprintf "Ignoring test assembly: %s" assembly)
                0
            else
                let args = 
                    buildDefinition.ApiDocGenArguments
                    |> replace "%1"  assemblyFile
                    |> replace "%2" (Path.GetDirectoryName(assemblyProjectMap.[assembly]))

                printfn "Running %s with %s" buildDefinition.ApiDocGenExecutable args

                Process.shellExec { Program = buildDefinition.ApiDocGenExecutable
                                    Args = []
                                    WorkingDir =  projectRoot
                                    CommandLine = args }
        
        let ret = 
            assemblyProjectMap.Keys
            |> Seq.map genApiDoc
            |> Seq.forall(fun x -> x = 0)

        match ret with
        | true -> ()
        | false -> failwith "ApiDoc generation failed"
    )

    let Commit = (fun _ ->
        if buildRequest.CheckInComment |> String.IsNullOrEmpty then
            failwith "!! NO CHECKIN COMMENT PROVIDED !!"
    
        let isExcluded file =
            buildRequest.FilesExcludedFromCheckIn
            |> Seq.exists ((=) file)

        let files =
            PGit.PendingChanges projectRoot
            |> Seq.filter (isExcluded >> not)
            |> List.ofSeq

        files
        |> Seq.iter (sprintf "Committing file %s" >> Trace.trace)

        PGit.Commit projectRoot (files, buildRequest.CheckInComment, buildDefinition.User.Login, buildDefinition.User.EMail)
    )

    let Push = (fun _ ->
        if buildDefinition.User.Password = null then
            failwith "!! NO PASSWORD PROVIDED !!"
    
        PGit.Push projectRoot (buildDefinition.User.Login, buildDefinition.User.Password.ToUnsecureString())
    )

    let AssemblyInfo = (fun _ ->
        let release = getChangeLog()
    
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

    let runFakeScript scriptFile args =
        let config = FakeRuntime.createConfigSimple VerboseLevel.Normal [] scriptFile args true false

        // unfort. FAKE 5 currently only supports Paket references so we have to hack here to 
        // get our local references loaded
        do
            let localReferences =
                [
                    Directory.GetFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "*.dll")
                    Directory.GetFiles(Path.GetDirectoryName(scriptFile), "*.dll")
                ]
                |> Seq.concat
                |> Seq.map Runners.AssemblyInfo.ofLocation
                |> List.ofSeq

            let t = config.RuntimeOptions.GetType()
            let f = t.GetField("_RuntimeDependencies@", BindingFlags.NonPublic ||| BindingFlags.Instance)
            f.SetValue(config.RuntimeOptions, localReferences)

        let prepared = FakeRuntime.prepareFakeScript config

        let runResult, cache, context = FakeRuntime.runScript prepared

        match runResult with
        | Runners.RunResult.SuccessRun warnings ->
            if warnings <> "" then
                traceFAKE "%O" warnings
            0
        | Runners.RunResult.CompilationError err ->
            let indentString num (str:string) =
                let indentString = String('\t', num)
                let splitMsg = str.Split([|"\r\n"; "\n"|], StringSplitOptions.None)
                indentString + String.Join(sprintf "%s%s" Environment.NewLine indentString, splitMsg)

            printfn "Script is not valid:"
            printfn "%s" err.FormattedErrors
            1
        | Runners.RunResult.RuntimeError err ->
            printfn "Script reported an error:"
            printfn "%A" err
            1

    let runScript (script:string) (args:string) =
        let ret = 
            if script.EndsWith(".fsx", StringComparison.OrdinalIgnoreCase) then
                runFakeScript script (args.Split( [| ' ' |] ) |> List.ofArray)
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


    let private getPackagingScript() =
        let script = projectRoot </> buildDefinition.PackagingScript
        if script |> File.Exists |> not then
            failwithf "Packaging script does not exist: %s" buildDefinition.PackagingScript
        script

    let CreatePackage = (fun _ ->
        let script = getPackagingScript()    
        runScript script buildDefinition.CreatePackageArguments
    )

    let DeployPackage = (fun _ ->
        let script = getPackagingScript()        
        runScript script buildDefinition.DeployPackageArguments
    )

    let PublishPackage = (fun _ ->
        let script = getPackagingScript()    
        runScript script buildDefinition.PublishPackageArguments
    )

module Runtime =
    open Fake.Core.TargetOperators

    let ScriptRun() =
        Target.create "All" Targets.All

        Target.create "Clean" Targets.Clean

        Target.create "Build" Targets.Build

        Target.create "RunTests" Targets.RunTests

        Target.create "GenerateApiDoc" Targets.GenerateApiDoc

        Target.create "Commit" Targets.Commit

        Target.create "Push" Targets.Push

        Target.create "AssemblyInfo" Targets.AssemblyInfo

        Target.create "CreatePackage" Targets.CreatePackage

        Target.create "DeployPackage" Targets.DeployPackage

        Target.create "PublishPackage" Targets.PublishPackage

        "Clean"
            =?> ("AssemblyInfo", changeLogFile |> File.Exists)
            ==> "Build"
            =?> ("GenerateApiDoc", buildDefinition.GenerateAPIDoc)
            =?> ("RunTests", buildDefinition.RunTests)
            =?> ("Commit", buildDefinition.CheckIn)
            =?> ("Push", buildDefinition.Push)
            =?> ("CreatePackage", buildDefinition.CreatePackage)
            =?> ("DeployPackage", buildDefinition.DeployPackage)
            =?> ("PublishPackage", buildDefinition.PublishPackage)
            ==> "All"
            |> ignore
    
        Target.runOrDefault "All"
