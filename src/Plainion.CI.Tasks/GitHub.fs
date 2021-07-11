module Plainion.CI.Tasks.PGitHub

open Plainion.CI
open Fake.Tools.Git
open PGit

type GitHubReleaseRequest = {
    ProjectRoot : string
    User : User
    ProjectName : string
    Files : string list
} 

/// Publishes a new release to GitHub with the current version of ChangeLog.md and
/// the given files
let Release request =
    if request.User.Password = null then
        failwith "!! NO PASSWORD PROVIDED !!"
    
    let release = request.ProjectRoot |> GetChangeLog 

    let user = request.User.Login
    let pwd = request.User.Password.ToUnsecureString()

    let version = release |> Option.map(fun x -> x.AssemblyVersion) |? defaultAssemblyVersion

    try
        Branches.deleteTag "" version
    with | _ -> ()
        
    Branches.tag "" version

    {
        GitPushRequest.ProjectRoot = request.ProjectRoot
        User = request.User
    }
    |> PGit.Push 
    
    // release on GitHub
        
    let releaseNotes =  release
                        |> Option.map(fun x -> x.Notes)
                        |? []

    let prerelease = (release |> Option.map(fun x -> x.SemVer.PreRelease) |> Option.isSome)

    FromFake.Octokit.createClient user pwd 
    |> FromFake.Octokit.makeRelease true user request.ProjectName version prerelease releaseNotes
    |> FromFake.Octokit.uploadFiles request.Files  
    |> FromFake.Octokit.releaseDraft
    |> Async.RunSynchronously


