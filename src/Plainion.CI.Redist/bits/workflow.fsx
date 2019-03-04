#load "PlainionCI.fsx"

open System
open System.IO
open Plainion.CI
open Plainion.CI.Tasks
open PlainionCI
open Fake.Core
open Fake.Core.TargetOperators
open Fake.DotNet
open Fake.DotNet.NuGet
open Fake.DotNet.Testing
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators

Target.create "All" (fun _ ->
    Trace.trace "--- Plainion.CI - DONE ---"
)

Target.create "Clean" (fun _ ->
    Shell.cleanDir outputPath
)

Target.create "Build" (fun _ ->
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

    let verifyAssemblyInfoIsIncludedInProject projectFile assemblyInfoFile =
        let assemblyInfoIsIncluded =
            projectFile
            |> File.ReadAllLines
            |> Seq.exists (contains assemblyInfoFile)
        if assemblyInfoIsIncluded then () else failwithf "AssemblyInfo file NOT included in project %s" projectFile

    !! ( projectRoot </> "src/**/*.??proj" )
    |> Seq.map getProjectDetails
    |> Seq.iter (fun (projFileName, projectName, folderName, attributes) ->
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
              CommandLine = (args + " --fsiargs \"--define:FAKE\" " + script ) }
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
    | _-> failwithf "script execution failed: %s" script


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
