# Plainion.CI

Provides tools for build automation and continuous integration

## Motivation

Nowadays continuous delivery is a must for every project. Short cycle times for new features and 
bug fixes - from code change till a new release is published - are important. Automation is the key.

Also there are many really good continuous intergration and delivery solutions already out there - 
if you are like me and have several one-man projects most existing solutions seem to be overkill.

This project puts the focus on simplicity. It aims to reduce the work to publish a new release of
your project to the push of a button.

Of course simplicitly comes with reduced flexibility. If your project grows, if the number of contributors
increases I encourage you to switch to one of the "bigger solutions" out there.

## Usage

Just start the tool and enter all relevant information under the "build definition" tab.
Then change to CheckIn tab, select the files you want to commit and enter a commit message and press "go".

![](doc/Overview.png)

### Hints

* specify package creation and deployment scripts relative to project root

### Custom packaging scripts

Here is an example for coding custom targets using fake

```F#
#r "/bin/Plainion.CI/FAKE/FakeLib.dll"
#load "/bin/Plainion.CI/bits/PlainionCI.fsx"

open Fake
open PlainionCI

Target "CreatePackage" (fun _ ->
    !! ( outputPath </> "*.*Tests.*" )
    ++ ( outputPath </> "*nunit*" )
    |> DeleteFiles
)

Target "DeployPackage" (fun _ ->
    let releaseDir = @"\bin\Plainion.CI"

    DeleteDir releaseDir

    CopyRecursive outputPath releaseDir true |> ignore
)

RunTarget()
```