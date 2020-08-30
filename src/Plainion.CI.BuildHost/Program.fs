open System.IO
open Fake.Runtime
open System.Reflection

[<EntryPoint>]
let main argv =
    // Hint: we currently need this reference to "Plainion.CI.Tasks" here to not get:
    // "System.PlatformNotSupportedException: Windows Data Protection API (DPAPI) is not supported on this platform"
    // reason unclear :(
    Plainion.CI.Common.projectName |> printfn "Preparing build workflow for '%s' ..."

    // disable unnecessary warning
    Environment.setEnvironVar "FAKE_ALLOW_NO_DEPENDENCIES" "true"

    let home = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
    let scriptFile = Path.Combine(home, "Workflow.fsx");
    
    printfn "Executing ..."
    Plainion.CI.Targets.runFakeScript scriptFile []