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
let Release (getChangeLog:GetChangeLog) (buildDefinition:BuildDefinition) projectRoot projectName files =
    if buildDefinition.User.Password = null then
        failwith "!! NO PASSWORD PROVIDED !!"
    
    let release = getChangeLog()

    let user = buildDefinition.User.Login
    let pwd = buildDefinition.User.Password.ToUnsecureString()

    try
        Branches.deleteTag "" release.NugetVersion
    with | _ -> ()
        
    Branches.tag "" release.NugetVersion
    PGit.Push projectRoot (user, pwd)
    
    // release on GitHub
        
    let releaseNotes =  release.Notes 
                        |> List.ofSeq

    createDraft user pwd projectName release.NugetVersion (release.SemVer.PreRelease <> None) releaseNotes 
    |> uploadFiles files  
    |> releaseDraft
    |> Async.RunSynchronously

