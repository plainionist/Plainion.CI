module Plainion.CI.Tasks.PGit

open System
open System.IO
open LibGit2Sharp
open Plainion.CI
open Fake.Core
open Fake.IO

module API = 
    /// Returns all non-ignored pending changes
    let GetPendingChanges projectRoot =
        use repo = new Repository( projectRoot )

        repo.RetrieveStatus()
        |> Seq.filter(fun e -> e.State.HasFlag( FileStatus.Ignored ) |> not )
        |> Seq.map(fun e -> e.FilePath)
        |> List.ofSeq

type GitPushRequest = {
    ProjectRoot : string
    User : User
}

/// Pushes the local repository to the default remote one
let Push request =
    // there are currently 2 blocking issues open with libgit2sharp and push related to windows and network:
    // - https://github.com/libgit2/libgit2sharp/issues/1429
    // - https://github.com/libgit2/libgit2/issues/4546
    // therefore we use a the command line "git" if found
    let cmdLineGit =
        Environment.GetEnvironmentVariable("PATH").Split([|';'|])
        |> Seq.map(fun path -> path.Trim())
        |> Seq.map(fun path -> Path.Combine(path, "git.exe"))
        |> Seq.tryFind File.Exists

    use repo = new Repository( request.ProjectRoot )
    let origin = repo.Network.Remotes.[ "origin" ]

    match cmdLineGit with
    | Some exe -> 
        let uri =
            if request.User.Password = null then
                // we assume we work with Private Access Token (PAT) instead of password
                origin.Url
            else
                // "https://github.com/plainionist/Plainion.CI.git"
                // https://stackoverflow.com/questions/29776439/username-and-password-in-command-for-git-push
                let uri = new Uri(origin.Url)
                let builder = new UriBuilder(uri)
                builder.UserName <- request.User.Login
                builder.Password <- request.User.Password.ToUnsecureString()
                builder.Uri.ToString()

        let ret =
            { Program = exe
              Args = []
              WorkingDir = request.ProjectRoot
              CommandLine = sprintf "%s %s" "push" uri }
            |> Process.shellExec 

        if ret <> 0 then
            failwith "Failed to push using command line git.exe"
    | None ->
        let options = new PushOptions()
        options.CredentialsProvider <- (fun url usernameFromUrl types -> let credentials = new UsernamePasswordCredentials()
                                                                         credentials.Username <- request.User.Login
                                                                         credentials.Password <- request.User.Password.ToUnsecureString()
                                                                         credentials :> Credentials)

        repo.Network.Push( origin, @"refs/heads/master", options )
    
type GitCommitRequest = {
    ProjectRoot : string
    User : User
    CheckInComment : string
    FilesExcludedFromCheckIn : string []
} 

let Commit request = 
    if request.CheckInComment |> String.IsNullOrEmpty then
        failwith "!! NO CHECKIN COMMENT PROVIDED !!"
    
    let isExcluded file =
        request.FilesExcludedFromCheckIn
        |> Seq.exists ((=) file)

    let files =
        request.ProjectRoot
        |> API.GetPendingChanges
        |> Seq.filter (isExcluded >> not)
        |> List.ofSeq

    files
    |> Seq.iter (sprintf "Committing file %s" >> Trace.trace)

    use repo = new Repository(request.ProjectRoot) 

    files
    |> Seq.iter(fun file -> Commands.Stage(repo, file))

    let author = new Signature(request.User.Login, request.User.EMail, DateTimeOffset.Now)

    repo.Commit(request.CheckInComment, author, author) |> ignore
