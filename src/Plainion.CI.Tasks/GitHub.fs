module Plainion.CI.Tasks.PGitHub

open Plainion.CI
open Fake.Tools.Git
open PGit

[<AutoOpen>]
module private Impl =
    let createDraft owner pwd project version prerelease notes = 
        FromFake.Octokit.createClient owner pwd
        |> FromFake.Octokit.makeRelease true owner project version prerelease notes

    let releaseDraft = FromFake.Octokit.releaseDraft

    let uploadFiles = FromFake.Octokit.uploadFiles

type GitHubReleaseRequest = {
    ProjectRoot : string
    User : User
    ProjectName : string
    Files : string []
} with 
    static member Create (def:BuildDefinition, files) =
        {
            ProjectRoot = def.RepositoryRoot
            User = def.User
            ProjectName = def.GetProjectName()
            Files = files
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

    createDraft user pwd request.ProjectName version (release |> Option.map(fun x -> x.SemVer.PreRelease) |> Option.isSome) releaseNotes 
    |> uploadFiles request.Files  
    |> releaseDraft
    |> Async.RunSynchronously

