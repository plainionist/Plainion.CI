module internal FromFake

// TODO: remove once Fake officially supports VS 2019
module MsBuild =
    open System.IO
    open Fake.Core
    open Fake.IO
    open Fake.IO.FileSystemOperators

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

// TODO: The content of this file was originally borrowed from https://github.com/fsharp/FAKE/blob/master/modules/Octokit/Octokit.fsx
// Hint: Octokit currently does not have signed assemblies so we have to put this code here - we would like to put it into Plainion.CI.Tasks
module Octokit =
    open Octokit    
    open Octokit.Internal
    open System
    open System.Threading
    open System.Net.Http
    open System.Reflection
    open System.IO

    type Draft =
        { Client : GitHubClient
          Owner : string
          Project : string
          DraftRelease : Release }

    // wrapper re-implementation of HttpClientAdapter which works around
    // known Octokit bug in which user-supplied timeouts are not passed to HttpClient object
    // https://github.com/octokit/octokit.net/issues/963
    type private HttpClientWithTimeout(timeout : TimeSpan) as this =
        inherit HttpClientAdapter(fun () -> HttpMessageHandlerFactory.CreateDefault())
        let setter = lazy(
            match typeof<HttpClientAdapter>.GetField("_http", BindingFlags.NonPublic ||| BindingFlags.Instance) with
            | null -> ()
            | f ->
                match f.GetValue(this) with
                | :? HttpClient as http -> http.Timeout <- timeout
                | _ -> ())

        interface IHttpClient with
            member __.Send(request : IRequest, ct : CancellationToken) =
                setter.Force()
                match request with :? Request as r -> r.Timeout <- timeout | _ -> ()
                base.Send(request, ct)

    let private isRunningOnMono = System.Type.GetType ("Mono.Runtime") <> null

    /// A version of 'reraise' that can work inside computation expressions
    let private captureAndReraise ex =
        System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex).Throw()
        Unchecked.defaultof<_>

    /// Retry the Octokit action count times
    let rec private retry count asyncF =
        // This retry logic causes an exception on Mono:
        // https://github.com/fsharp/fsharp/issues/440
        if isRunningOnMono then
            asyncF
        else
            async {
                try
                    return! asyncF
                with ex ->
                    return!
                        match (ex, ex.InnerException) with
                        | (:? AggregateException, (:? AuthorizationException as ex)) -> captureAndReraise ex
                        | _ when count > 0 -> retry (count - 1) asyncF
                        | (ex, _) -> captureAndReraise ex
            }

    /// Retry the Octokit action count times after input succeed
    let private retryWithArg count input asycnF =
        async {
            let! choice = input |> Async.Catch
            match choice with
            | Choice1Of2 input' ->
                return! (asycnF input') |> retry count
            | Choice2Of2 ex ->
                return captureAndReraise ex
        }

    let createClient user (password:string) =
        async {
            let httpClient = new HttpClientWithTimeout(TimeSpan.FromMinutes 20.)
            let connection = new Connection(new ProductHeaderValue("FAKE"), httpClient)
            let github = new GitHubClient(connection)
            github.Credentials <- Credentials(user, password)
            return github
        }

    let makeRelease draft owner project version prerelease (notes:seq<string>) (client : Async<GitHubClient>) =
        retryWithArg 5 client <| fun client' -> async {
            printfn "Creating release id ..."
            let data = new NewRelease(version)
            data.Name <- version
            data.Body <- String.Join(Environment.NewLine, notes)
            data.Draft <- draft
            data.Prerelease <- prerelease
            let! draft = Async.AwaitTask <| client'.Repository.Release.Create(owner, project, data)
            let draftWord = if data.Draft then " draft" else ""
            printfn "Created %s release id %d" draftWord draft.Id
            return {
                Client = client'
                Owner = owner
                Project = project
                DraftRelease = draft }
        }

    let uploadFile fileName (draft : Async<Draft>) =
        retryWithArg 5 draft <| fun draft' -> async {
            let fi = FileInfo(fileName)
            let archiveContents = File.OpenRead(fi.FullName)
            let assetUpload = new ReleaseAssetUpload(fi.Name,"application/octet-stream",archiveContents,Nullable<TimeSpan>())
            let! asset = Async.AwaitTask <| draft'.Client.Repository.Release.UploadAsset(draft'.DraftRelease, assetUpload)
            printfn "Uploaded %s" asset.Name
            return draft'
        }

    let uploadFiles fileNames (draft : Async<Draft>) = async {
        let! draft' = draft
        let draftW = async { return draft' }
        let! _ = Async.Parallel [for f in fileNames -> uploadFile f draftW ]
        return draft'
    }

    let releaseDraft (draft : Async<Draft>) =
        retryWithArg 5 draft <| fun draft' -> async {
            printfn "Releasing draft ..."
            let update = draft'.DraftRelease.ToUpdate()
            update.Draft <- Nullable<bool>(false)
            let! released = Async.AwaitTask <| draft'.Client.Repository.Release.Edit(draft'.Owner, draft'.Project, draft'.DraftRelease.Id, update)
            printfn "Released %d on github" released.Id
        }
