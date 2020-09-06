[<AutoOpen>]
module Plainion.CI.Tasks.Foundation

open System
open Fake.Core
open Fake.IO
open Fake.IO.FileSystemOperators

let replace (oldValue:string) (newValue:string) (str:string) = 
    str.Replace(oldValue,newValue)

let contains (substring:string) (str:string) =
    str.IndexOf(substring, StringComparison.OrdinalIgnoreCase) <> -1

let (|?) = defaultArg

let defaultAssemblyVersion = "1.0.0"

let GetChangeLog = 
    let mutable releaseNotes : Fake.Core.ReleaseNotes.ReleaseNotes option = None
    fun (projectRoot:string) ->
        if releaseNotes |> Option.isNone then
            releaseNotes <-
                match projectRoot </> "ChangeLog.md" |> File.exists with
                | true -> projectRoot </> "ChangeLog.md" |> ReleaseNotes.load |> Some
                | false -> None
        releaseNotes
