module Plainion.CI.Tasks.PGitHub

open System
open Plainion.CI
open Fake.Tools.Git
open Fake.Api
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
    if request.User.Password = null && request.User.PAT = null then
        failwith "!! NEITHER PASSWORD NOR PAT PROVIDED !!"
    
    let release = request.ProjectRoot |> GetChangeLog 

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

    // see: https://fake.build/apidocs/v5/fake-api-github.html

    let user = request.User.Login

    if request.User.Password <> null then
        let pwd = request.User.Password.ToUnsecureString()
        GitHub.createClient user pwd 
    else
        Environment.ExpandEnvironmentVariables(request.User.PAT)
        |> GitHub.createClientWithToken
    |> GitHub.draftNewRelease user request.ProjectName version prerelease releaseNotes
    |> GitHub.uploadFiles request.Files  
    |> GitHub.publishDraft
    |> Async.RunSynchronously


