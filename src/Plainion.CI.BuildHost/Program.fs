open System
open System.IO
open Fake.Runtime
open Fake.Runtime.Trace
open System.Reflection

[<EntryPoint>]
let main argv =
    // trick to ensure Plainion.CI.Tasks is loaded
    Plainion.CI.Common.projectName |> printfn "Building project: %s"

    printfn "### config ###"
    let home = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
    let scriptFile = Path.Combine(home, "bits", "Workflow.fsx");
    let config = FakeRuntime.createConfigSimple VerboseLevel.Verbose [] scriptFile [] true false

    // unfort. FAKE 5 currently only supports Paket references so we have to hack here to 
    // get our local references loaded
    do
        let localReferences =
            Directory.GetFiles(home, "*.dll")
            |> Seq.map Runners.AssemblyInfo.ofLocation
            |> List.ofSeq

        let t = config.RuntimeOptions.GetType()
        let f = t.GetField("_RuntimeDependencies@", BindingFlags.NonPublic ||| BindingFlags.Instance)
        f.SetValue(config.RuntimeOptions, localReferences)

    printfn "### prepare ###"
    let prepared = FakeRuntime.prepareFakeScript config

    printfn "### run ###"
    let runResult, cache, context = FakeRuntime.runScript prepared

    match runResult with
    | Runners.RunResult.SuccessRun warnings ->
        if warnings <> "" then
            traceFAKE "%O" warnings
        0
    | Runners.RunResult.CompilationError err ->
        let indentString num (str:string) =
            let indentString = String('\t', num)
            let splitMsg = str.Split([|"\r\n"; "\n"|], StringSplitOptions.None)
            indentString + String.Join(sprintf "%s%s" Environment.NewLine indentString, splitMsg)

        printfn "Script is not valid:"
        printfn "%s" err.FormattedErrors
        1
    | Runners.RunResult.RuntimeError err ->
        printfn "Script reported an error:"
        printfn "%A" err
        1