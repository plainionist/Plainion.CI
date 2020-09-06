module Plainion.CI.Tasks.PGitHub

open Plainion.CI
open Fake.Tools.Git

[<AutoOpen>]
module private Impl =
    let createDraft owner pwd project version prerelease notes = 
        FromFake.Octokit.createClient owner pwd
        |> FromFake.Octokit.makeRelease true owner project version prerelease notes

    let releaseDraft = FromFake.Octokit.releaseDraft

    let uploadFiles = FromFake.Octokit.uploadFiles


/// Publishes a new release to GitHub with the current version of ChangeLog.md and
/// the given files
let Release (buildDefinition:BuildDefinition) projectRoot projectName files =
    if buildDefinition.User.Password = null then
        failwith "!! NO PASSWORD PROVIDED !!"
    
    let release = projectRoot |> GetChangeLog 

    let user = buildDefinition.User.Login
    let pwd = buildDefinition.User.Password.ToUnsecureString()

    let version = release |> Option.map(fun x -> x.AssemblyVersion) |? defaultAssemblyVersion

    try
        Branches.deleteTag "" version
    with | _ -> ()
        
    Branches.tag "" version
    buildDefinition
    |> PGit.GitPushRequest.Create
    |> PGit.Push 
    
    // release on GitHub
        
    let releaseNotes =  release
                        |> Option.map(fun x -> x.Notes)
                        |? []

    createDraft user pwd projectName version (release |> Option.map(fun x -> x.SemVer.PreRelease) |> Option.isSome) releaseNotes 
    |> uploadFiles files  
    |> releaseDraft
    |> Async.RunSynchronously

