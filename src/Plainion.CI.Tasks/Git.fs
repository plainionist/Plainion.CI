﻿module Plainion.CI.Tasks.PGit

open System
open LibGit2Sharp

let Commit workspaceRoot ((files:string list), comment, name, email) =
    use repo = new Repository( workspaceRoot ) 

    files
    |> Seq.iter(fun file -> repo.Stage( file ) )

    let author = new Signature( name, email, DateTimeOffset.Now )

    repo.Commit( comment, author, author ) |> ignore

let Push workspaceRoot (name, password) =
    use repo = new Repository( workspaceRoot )

    let options = new PushOptions()
    options.CredentialsProvider <- (fun url usernameFromUrl types -> let credentials = new UsernamePasswordCredentials()
                                                                     credentials.Username <- name
                                                                     credentials.Password <- password
                                                                     credentials :> Credentials)

    repo.Network.Push( repo.Network.Remotes.[ "origin" ], @"refs/heads/master", options )

let PendingChanges workspaceRoot =
    use repo = new Repository( workspaceRoot )

    repo.RetrieveStatus()
    |> Seq.filter(fun e -> e.State.HasFlag( FileStatus.Ignored ) |> not )
    |> Seq.map(fun e -> e.FilePath)
    |> List.ofSeq
