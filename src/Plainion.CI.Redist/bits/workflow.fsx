#load "PlainionCI.fsx"

open System
open System.IO
open Fake
open Fake.Testing.NUnit3
open Fake.AssemblyInfoFile
open Fake.ReleaseNotesHelper
open Plainion.CI
open Plainion.CI.Tasks
open PlainionCI

Target "All" (fun _ ->
    trace "--- Plainion.CI - DONE ---"
)

Target "Clean" (fun _ ->
    CleanDir outputPath
)

Target "Build" (fun _ ->
    build setParams (buildDefinition.GetSolutionPath())
)

Target "RestoreNugetPackages" (fun _ ->
    buildDefinition.GetSolutionPath()
    |> RestoreMSSolutionPackages (fun p ->
         { p with
             OutputPath = projectRoot </> "packages"
             Retries = 1 })
)

Target "RunNUnitTests" (fun _ ->
    let assemblies = !! ( outputPath </> buildDefinition.TestAssemblyPattern )
    let toolPath = Path.GetDirectoryName( buildDefinition.TestRunnerExecutable )

    if fileExists ( toolPath </> "nunit-console.exe" ) then
        assemblies
        // "parallel" version does not show test output
        |> NUnit (fun p -> 
            { p with
                ToolPath = toolPath
                DisableShadowCopy = true })
    else
        assemblies
        |> NUnit3 (fun p -> 
            { p with
                ToolPath = toolPath </> "nunit3-console.exe"
                ShadowCopy = false })
)

Target "GenerateApiDoc" (fun _ ->
    if File.Exists buildDefinition.ApiDocGenExecutable |> not then
        failwithf "!! ApiDocGenExecutable not found: %s !!" buildDefinition.ApiDocGenExecutable

    let projects = PMsBuild.GetProjectFiles(buildDefinition.GetSolutionPath())

    let assemblyToSourcesMap = 
        projects
        |> Seq.map PMsBuild.LoadProject
        |> Seq.map(fun proj -> proj.Assembly, proj.Location)
        |> dict

    let getAssemblySources assembly =
        Path.GetDirectoryName(assemblyToSourcesMap.[assembly])

    let genApiDoc assembly =
        let relevantAssembly =
            !!( outputPath </> assembly)
            -- ( outputPath </> buildDefinition.TestAssemblyPattern )
            |> List.ofSeq

        match relevantAssembly with
        | [] -> trace (sprintf "Ignoring test assembly: %s" assembly)
                0
        | [x] ->
            let args = 
                buildDefinition.ApiDocGenArguments
                |> replace "%1"  (outputPath </> assembly)
                |> replace "%2" (getAssemblySources assembly)

            printfn "Running %s with %s" buildDefinition.ApiDocGenExecutable args

            shellExec { Program = buildDefinition.ApiDocGenExecutable
                        Args = []
                        WorkingDirectory =  projectRoot
                        CommandLine = args}
        | _ -> failwith "Only one assembly expected"    
        
    let ret = 
        assemblyToSourcesMap.Keys
        |> Seq.map genApiDoc
        |> Seq.forall(fun x -> x = 0)

    match ret with
    | true -> ()
    | false -> failwith "ApiDoc generation failed"
)

Target "Commit" (fun _ ->
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
    |> Seq.iter (sprintf "Commiting file %s" >> trace)

    PGit.Commit projectRoot (files, buildRequest.CheckInComment, buildDefinition.User.Login, buildDefinition.User.EMail)
)

Target "Push" (fun _ ->
    if buildDefinition.User.Password = null then
        failwith "!! NO PASSWORD PROVIDED !!"
    
    PGit.Push projectRoot (buildDefinition.User.Login, buildDefinition.User.Password.ToUnsecureString())
)

Target "AssemblyInfo" (fun _ ->
    let release = getChangeLog()
    
    let getAssemblyInfoAttributes vsProjName =
        [ Attribute.Title (vsProjName)
          Attribute.Product projectName
          Attribute.Description projectName
          Attribute.Copyright (sprintf "Copyright @ %i" DateTime.UtcNow.Year)
          Attribute.Version release.AssemblyVersion
          Attribute.FileVersion release.AssemblyVersion ]

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

    !! ( projectRoot </> "src/**/*.??proj" )
    |> Seq.map getProjectDetails
    |> Seq.iter (fun (projFileName, projectName, folderName, attributes) ->
        match projFileName with
        | Fsproj -> CreateFSharpAssemblyInfo (folderName </> "AssemblyInfo.fs") attributes
        | Csproj -> CreateCSharpAssemblyInfo ((folderName </> "Properties") </> "AssemblyInfo.cs") attributes
        )
)

let runScript (script:string) args =
    let ret = 
        if script.EndsWith(".fsx", StringComparison.OrdinalIgnoreCase) then
            { Program = "fake.exe"
              Args = []
              WorkingDirectory = projectRoot
              CommandLine = (args + " --fsiargs \"--define:FAKE\" " + script ) }
            |> shellExec
        elif script.EndsWith(".msbuild", StringComparison.OrdinalIgnoreCase) || script.EndsWith(".targets", StringComparison.OrdinalIgnoreCase) then
            { Program = @"C:\Program Files (x86)\MSBuild\12.0\Bin\MSBuild.exe"
              Args = []
              WorkingDirectory = projectRoot
              CommandLine = (sprintf "/p:OutputPath=%s %s %s" outputPath args script) }
            |> shellExec 
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

Target "CreatePackage" (fun _ ->
    let script = getPackagingScript()    
    runScript script buildDefinition.CreatePackageArguments
)

Target "DeployPackage" (fun _ ->
    let script = getPackagingScript()        
    runScript script buildDefinition.DeployPackageArguments
)

Target "PublishPackage" (fun _ ->
    let script = getPackagingScript()    
    runScript script buildDefinition.PublishPackageArguments
)

"Clean"
    ==> "RestoreNugetPackages"
    =?> ("AssemblyInfo", changeLogFile |> File.Exists)
    ==> "Build"
    =?> ("GenerateApiDoc", buildDefinition.GenerateAPIDoc)
    =?> ("RunNUnitTests", buildDefinition.RunTests)
    =?> ("Commit", buildDefinition.CheckIn)
    =?> ("Push", buildDefinition.Push)
    =?> ("CreatePackage", buildDefinition.CreatePackage)
    =?> ("DeployPackage", buildDefinition.DeployPackage)
    =?> ("PublishPackage", buildDefinition.PublishPackage)
    ==> "All"

RunTarget()
