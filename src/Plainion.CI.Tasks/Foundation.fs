[<AutoOpen>]
module Plainion.CI.Tasks.Foundation

open System

let replace (oldValue:string) (newValue:string) (str:string) = 
    str.Replace(oldValue,newValue)

let contains (substring:string) (str:string) =
    str.IndexOf(substring, StringComparison.OrdinalIgnoreCase) <> -1

