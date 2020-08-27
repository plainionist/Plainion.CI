#load "PlainionCI.fsx"

open System
open System.IO
open Plainion.CI
open Plainion.CI.Tasks
open PlainionCI
open Fake.Core
open Fake.Core.TargetOperators
open Fake.DotNet
open Fake.DotNet.Testing
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators

module private FromFake =
    type MsBuildEntry = {
        Version: string;
        Paths: string list;
    }

    let knownMsBuildEntries =
        [
            { Version = "16.0"; Paths = [@"\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin"
                                         @"\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin"
                                         @"\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin"
                                         @"\MSBuild\Current\Bin"
                                         @"\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin"] }
            { Version = "15.0"; Paths = [@"\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin"
                                         @"\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin"
                                         @"\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin"
                                         @"\MSBuild\15.0\Bin"
                                         @"\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin"] }
            { Version = "14.0"; Paths = [@"\MSBuild\14.0\Bin"] }
            { Version = "12.0"; Paths = [@"\MSBuild\12.0\Bin"; @"\MSBuild\12.0\Bin\amd64"] }
        ]

    let oldMsBuildLocations =
        [ @"c:\Windows\Microsoft.NET\Framework\v4.0.30319\";
          @"c:\Windows\Microsoft.NET\Framework\v4.0.30128\";
          @"c:\Windows\Microsoft.NET\Framework\v3.5\"
        ]

    let toDict items =
        items |> Seq.map (fun f -> f.Version, f.Paths) |> Map.ofSeq

    let getAllKnownPaths =
        (knownMsBuildEntries |> List.collect (fun m -> m.Paths)) @ oldMsBuildLocations

    let monoVersionToUseMSBuildOn = System.Version("5.0")

    let msBuildExe =
        /// the value we're given can be a:
        ///     * full path to a file or
        ///     * just a directory
        /// if just a directory we can make it the path to a file by Path-Combining the tool name to the directory.
        let exactPathOrBinaryOnPath tool input =
            if Directory.Exists input
            then input </> tool
            else input

        let which tool = ProcessUtils.tryFindFileOnPath tool
        let msbuildEnvironVar = Fake.Core.Environment.environVarOrNone "MSBuild"

        let foundExe =
            match Fake.Core.Environment.isUnix, Fake.Core.Environment.monoVersion with
            | true, Some(_, Some(version)) when version >= monoVersionToUseMSBuildOn ->
                let sources = [
                    msbuildEnvironVar |> Option.map (exactPathOrBinaryOnPath "msbuild")
                    msbuildEnvironVar |> Option.bind which
                    which "msbuild"
                    which "xbuild"
                ]
                defaultArg (sources |> List.choose id |> List.tryHead) "msbuild"
            | true, _ ->
                let sources = [
                    msbuildEnvironVar |> Option.map (exactPathOrBinaryOnPath "xbuild")
                    msbuildEnvironVar |> Option.bind which
                    which "xbuild"
                    which "msbuild"
                ]
                defaultArg (sources |> List.choose id |> List.tryHead) "xbuild"
            | false, _ ->

                let tryFindFileInDirsThenPath paths tool =
                    match ProcessUtils.tryFindFile paths tool with
                    | Some path -> Some path
                    | None -> ProcessUtils.tryFindFileOnPath tool

                let configIgnoreMSBuild = None
                let findOnVSPathsThenSystemPath =
                    let dict = toDict knownMsBuildEntries
                    let vsVersionPaths =
                        defaultArg (Fake.Core.Environment.environVarOrNone "VisualStudioVersion" |> Option.bind dict.TryFind) getAllKnownPaths
                        |> List.map ((@@) Fake.Core.Environment.ProgramFilesX86)

                    tryFindFileInDirsThenPath vsVersionPaths "MSBuild.exe"

                let sources = [
                    msbuildEnvironVar |> Option.map (exactPathOrBinaryOnPath "MSBuild.exe")
                    msbuildEnvironVar |> Option.bind which
                    configIgnoreMSBuild
                    findOnVSPathsThenSystemPath
                ]
                defaultArg (sources |> List.choose id |> List.tryHead) "MSBuild.exe"

        if foundExe.Contains @"\BuildTools\" then
            printfn "If you encounter msbuild errors make sure you have copied the required SDKs, see https://github.com/Microsoft/msbuild/issues/1697"
        elif foundExe.Contains @"\2017\" then
            printfn "Using msbuild of VS2017 (%s), if you encounter build errors make sure you have installed the necessary workflows!" foundExe
        elif foundExe.Contains @"\2019\" then
            printfn "Using msbuild of VS2019 (%s), if you encounter build errors make sure you have installed the necessary workflows!" foundExe        
        foundExe

Target.create "All" (fun _ ->
    Trace.trace "--- Plainion.CI - DONE ---"
)

Target.create "Clean" (fun _ ->
    Shell.cleanDir outputPath
)

Target.create "Build" (fun _ ->

    let setParams (defaults:MSBuildParams) =
        { defaults with
            ToolPath = FromFake.msBuildExe
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

Target.create "RunTests" (fun _ ->
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

Target.create "GenerateApiDoc" (fun _ ->
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

Target.create "Commit" (fun _ ->
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

Target.create "Push" (fun _ ->
    if buildDefinition.User.Password = null then
        failwith "!! NO PASSWORD PROVIDED !!"
    
    PGit.Push projectRoot (buildDefinition.User.Login, buildDefinition.User.Password.ToUnsecureString())
)

Target.create "AssemblyInfo" (fun _ ->
    let release = getChangeLog()
    
    let getAssemblyInfoAttributes vsProjName =
        [ AssemblyInfo.Title (vsProjName)
          AssemblyInfo.Product projectName
          AssemblyInfo.Description projectName
          AssemblyInfo.Copyright (sprintf "Copyright @ %i" DateTime.UtcNow.Year)
          AssemblyInfo.Version release.AssemblyVersion
          AssemblyInfo.FileVersion release.AssemblyVersion ]

    let getProjectDetails projectPath =
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

let runScript (script:string) args =
    let ret = 
        if script.EndsWith(".fsx", StringComparison.OrdinalIgnoreCase) then
            { Program = "fake.exe"
              Args = []
              WorkingDir = projectRoot
              CommandLine = (args + " --fsiargs \"--define:FAKE\" --removeLegacyFakeWarning " + script ) }
            |> Process.shellExec
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

Target.create "CreatePackage" (fun _ ->
    let script = getPackagingScript()    
    runScript script buildDefinition.CreatePackageArguments
)

Target.create "DeployPackage" (fun _ ->
    let script = getPackagingScript()        
    runScript script buildDefinition.DeployPackageArguments
)

Target.create "PublishPackage" (fun _ ->
    let script = getPackagingScript()    
    runScript script buildDefinition.PublishPackageArguments
)

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

Target.runOrDefault ""
