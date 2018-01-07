---
title: Customization
navigation_weight: 1
---

# Customization

You can write custom package creation and deployment scripts either in FAKE or in MsBuild.

Here is an example for coding custom targets using FAKE:

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

You can create a NuGet package from your custom "CreatePackage" target like this:

```F#
    [
        ("Plainion.CI*", Some "lib", None)
    ]
    |> PNuGet.Pack (projectRoot </> "build" </> "Dummy.nuspec") (projectRoot </> "pkg")
```

Hint: this function assumes that you have a ChangeLog.md in the root of your project.

A sample NuSpec could look like this

```Xml
<?xml version="1.0"?>
<package >
  <metadata>
    <id>@project@</id>
    <title>@project@</title>
    <version>@build.number@</version>
    <authors>me</authors>
    <owners>also.me</owners>
    <licenseUrl>http://opensource.org/licenses/BSD-3-Clause</licenseUrl>
    <projectUrl>https://github.com/ronin4net/Plainion.CI</projectUrl>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <description>
      this is just a dummy template for testing
    </description>
    <releaseNotes>
      @releaseNotes@
    </releaseNotes>
    @dependencies
    <copyright>Copyright 2016</copyright>
  </metadata>
  @files@
</package>
```

You can publish a NuGet package like this 

```F#
PNuGet.Publish (projectRoot </> "pkg")
```

**Hint:** Publishing NuGet packages currently only works if you once followed the instructions [here](https://docs.nuget.org/ndocs/create-packages/publish-a-package) 
regarding APIKey and have stored your APIKey with "setApiKey":

```cmd
NuGet.exe setapikey <your api key> -source https://www.nuget.org/api/v2/package
```

You can publish a release to GitHub like this

```F#
PGitHub.Release [ zip ]
```

