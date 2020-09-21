open System
open System.IO
open System.Reflection
open Fake.Runtime
open Fake.Runtime.Trace

[<AutoOpen>]
module private Impl =
    let runFakeScript scriptFile args =
        let config = FakeRuntime.createConfigSimple VerboseLevel.Normal [] scriptFile args true false

        // unfort. FAKE 5 currently only supports Paket references so we have to hack here to 
        // get our local references loaded
        do
            let localReferences =
                [
                    Directory.GetFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "*.dll")
                    Directory.GetFiles(Path.GetDirectoryName(scriptFile), "*.dll")
                ]
                |> Seq.concat
                |> Seq.map Runners.AssemblyInfo.ofLocation
                |> List.ofSeq

            let t = config.RuntimeOptions.GetType()
            let f = t.GetField("_RuntimeDependencies@", BindingFlags.NonPublic ||| BindingFlags.Instance)
            f.SetValue(config.RuntimeOptions, localReferences)

        let prepared = FakeRuntime.prepareFakeScript config

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

[<EntryPoint>]
let main argv =
    match argv |> List.ofArray with
    | [] ->
        // Hint: we currently need this reference to "Plainion.CI.Tasks" here to not get:
        // "System.PlatformNotSupportedException: Windows Data Protection API (DPAPI) is not supported on this platform"
        // reason unclear :(
        Plainion.CI.Variables.projectName |> printfn "Preparing build workflow for '%s' ..."

        // disable unnecessary warning
        Environment.setEnvironVar "FAKE_ALLOW_NO_DEPENDENCIES" "true"

        let home = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
        let scriptFile = Path.Combine(home, "Workflow.fsx");
    
        printfn "Executing ..."
        runFakeScript scriptFile []
    | h::t -> runFakeScript h t