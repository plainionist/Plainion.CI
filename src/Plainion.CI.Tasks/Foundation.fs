[<AutoOpen>]
module Plainion.CI.Tasks.Foundation

open System
open System.Collections.Generic

let replace (oldValue:string) (newValue:string) (str:string) = 
    str.Replace(oldValue,newValue)

let contains (substring:string) (str:string) =
    str.IndexOf(substring, StringComparison.OrdinalIgnoreCase) <> -1

type GetChangeLog = unit -> Fake.Core.ReleaseNotes.ReleaseNotes
type GetAssemblyProjectMap = unit -> IDictionary<string,string>
