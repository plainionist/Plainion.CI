// The content of this file was originally borrowed from https://github.com/fsharp/FAKE/blob/master/modules/Octokit/Octokit.fsx
// Hint: Octokit currently does not have signed assemblies so we have to put this code here - we would like to put it into Plainion.CI.Tasks
module Plainion.CI.Tasks.PGitHub

#if FAKE
#r "../Octokit.dll"
#else
#r "../../../bin/Debug/Octokit.dll"
#endif
#r "System.Net.Http"

open Octokit

type Draft =
    { Client : GitHubClient
      Owner : string
      Project : string
      DraftRelease : Release }

module private FromFake =
    open Octokit.Internal
    open System
    open System.Threading
    open System.Net.Http
    open System.Reflection
    open System.IO

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

    let createClient user password =
        async {
            let httpClient = new HttpClientWithTimeout(TimeSpan.FromMinutes 20.)
            let connection = new Connection(new ProductHeaderValue("FAKE"), httpClient)
            let github = new GitHubClient(connection)
            github.Credentials <- Credentials(user, password)
            return github
        }

    let makeRelease draft owner project version prerelease (notes:seq<string>) (client : Async<GitHubClient>) =
        retryWithArg 5 client <| fun client' -> async {
            let data = new NewRelease(version)
            data.Name <- version
            data.Body <- String.Join(Environment.NewLine, notes)
            data.Draft <- draft
            data.Prerelease <- prerelease
            let! draft = Async.AwaitTask <| client'.Repository.Release.Create(owner, project, data)
            let draftWord = if data.Draft then " draft" else ""
            printfn "Created%s release id %d" draftWord draft.Id
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
            let update = draft'.DraftRelease.ToUpdate()
            update.Draft <- Nullable<bool>(false)
            let! released = Async.AwaitTask <| draft'.Client.Repository.Release.Edit(draft'.Owner, draft'.Project, draft'.DraftRelease.Id, update)
            printfn "Released %d on github" released.Id
        }

let createDraft owner pwd project version prerelease notes = 
    FromFake.createClient owner pwd
    |> FromFake.makeRelease true owner project version prerelease notes

let releaseDraft = FromFake.releaseDraft

let uploadFiles = FromFake.uploadFiles
