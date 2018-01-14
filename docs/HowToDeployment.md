---
title: HowTo configure the deployment steps?
navigation_weight: 2
---

# HowTo configure the deployment steps?

The concrete deployment steps are usually project specific so you have to define them.

Plainion.CI supports [FAKE](https://fake.build/) and MsBuild scripts but strongly recommends FAKE due to its power.

Plainion.CI provides various helpers for GitHub and NuGet out of the box.


## Deploying with FAKE

FAKE deployment scripts have to end with ".fsx".
To get started copy the following into your deployment script, e.g. "build\Targets.fsx":

```F#
#r "/bin/Plainion.CI/FAKE/FakeLib.dll"
#load "/bin/Plainion.CI/bits/PlainionCI.fsx"

open Fake
open PlainionCI

Target "CreatePackage" (fun _ ->
    !! ( outputPath </> "*.*Tests.*" )
    ++ ( outputPath </> "*nunit*" )
    ++ ( outputPath </> "*Moq*" )
    ++ ( outputPath </> "TestResult.xml" )
    ++ ( outputPath </> "**/*.pdb" )
    |> DeleteFiles

    PZip.PackRelease()
)

Target "Deploy" (fun _ ->
    let releaseDir = @"\bin\MyCoolProject"

    CleanDir releaseDir

    let zip = PZip.GetReleaseFile()
    Unzip releaseDir zip
)

RunTarget()
```

*Hint:* Don't forget to adjust the path in the first two lines to your Plainion.CI installation.

The script defines two targets. "CreatePackage" will delete "unwanted" files from your "bin" folder and 
then create a ZIP file under your "bin" folder.
"Deploy" will take the created ZIP and deploy it under the specified "releaseDir".

### Publishing to GitHub

In order to create a new release on GitHub add the following code

```F#
Target "Publish" (fun _ ->
    let zip = PZip.GetReleaseFile()
    PGitHub.Release [ zip ]
)
```

above

```F#
RunTarget()
```

### Publishing to NuGet

In order to create a new release on NuGet change the "CreatePackage" target to

```F#
Target "CreatePackage" (fun _ ->
    !! ( outputPath </> "*.*Tests.*" )
    ++ ( outputPath </> "*nunit*" )
    ++ ( outputPath </> "*Moq*" )
    ++ ( outputPath </> "TestResult.xml" )
    ++ ( outputPath </> "**/*.pdb" )
    |> DeleteFiles

    [
        ( projectName + ".*", Some "lib/NET45", None)
    ]
    |> PNuGet.Pack (projectRoot </> "build" </> projectName + ".nuspec") (projectRoot </> "pkg")
)
```

The "PNuGet.Pack" task will also generate a proper NuGet package spec from your template. Therefore 
copy the following template under "build/<projectname>.nuspec" and adjust it accordingly:

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

Then add the following code

```F#
Target "Publish" (fun _ ->
    PNuGet.PublishPackage projectName (projectRoot </> "pkg")
)
```

above

```F#
RunTarget()
```

*Hint:* Publishing NuGet packages currently only works if you once followed the instructions [here](https://docs.nuget.org/ndocs/create-packages/publish-a-package) 
regarding APIKey and have stored your APIKey with "setApiKey":

```cmd
NuGet.exe setapikey <your api key> -source https://www.nuget.org/api/v2/package
```

## Deployming with MsBuild

MsBuild deployment scripts have to end with ".msbuild".

Plainion.CI does no provide MsBuild specific tasks out of the box. 
