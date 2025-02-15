## 4.0 - 2025-02-15

- upgrade to .Net8
- upgrade of NuGet packages

## 3.3 - 2022-06-06

- all NuGet packages updated
- support for VS 2022 added 
- support for VS being installed in "Program Files" instead of "Program Files (x86)" added

## 3.2 - 2022-06-06

- Upgraded to .Net 6
- all NuGet packages updated

## 3.1 - 2021-09-03

- Git: if no password is given we assume a PAT is stored globally and try to push
  without any explicit credentials
- Git: if PAT is given we use this to push the changes to remote origin
- GitHub: support for PAT for creating releases added
- GitHub: prerelease handling fixed

## 3.0 - 2021-07-11

- Migrated to .Net 5
- GitHub.fsx moved to Tasks library
- Migrated to FAKE 5 (.Net core)
  - Plainion.CI.Redist removed
  - PlainionCI.fsx removed 
  - read [docs/Migration-to-v3.md] for more details
- APIs removed
  - getPropertyAndTrace
  - changeLogFile
  - getChangeLog
  - getAssemblyProjectMap

## 2.1 - 2020-08-25

- Support for .Net Core projects extended
  - don't fail if project does not reference AssemblyInfo file 
  - don't fail if project does not have "AssemblyName" or "OutputType" specified
  - get package dependencies from MS project "PackageReference" instead of packages.config
- Support for VS 2019 added

## 2.0 - 2019-03-04

- Updated to latest FAKE
- "nuget restore" no longer explicitly as this is now fully integrated in msbuild and 
  would download dependencies for .Net Core projects locally instead of centrally
- added support for other test runner than NUnit

## 1.12 - 2018-06-02

- Pass correct username and password to git.exe when pushing

## 1.11 - 2018-02-25

- Use git.exe from command line for push to workaroundlibgit2sharp issues
- Plainion.Core updated
- Plainion.Windows updated
- FAKE updated
- NuGet.CommandLine updated
- Libgit2Sharp updated
- Octokit updated
- NUnit updated

## 1.10 - 2017-11-15

- downgraded libgit2sharp to get http proxy support again

## 1.9 - 2017-08-24

- keep UI responsive even if many text is logged

## 1.8 - 2017-08-23

- fixed updating pending changes

## 1.7 - 2017-08-22

- dependencies updated to support VS 2017

## 1.6 - 2017-01-08

- exclude test assemblies from api doc generation
- files generated during build are automatically considered for check-in
- checked that generated assemblyInfo files are also included in projects
- added "Ignore" context menu entry
- support for ";" separated test assemblies added
- dependencies for nuspec automatically detected from the package.config files of the given assemblies

## 1.5 - 2017-01-03

- separated "deploy" and "publish" of releases

## 1.4 - 2017-01-03

- mapping assemblies to source folders fixed
- project name displayed in title

## 1.3 - 2017-01-03

- bullet list handling in changelog.md fixed
- support for IronDoc "-sources" switch added
- PNuGet.PublishPackage added

## 1.2 - 2017-01-01

- Help system (Build Definition) improved

## 1.1 - 2016-12-31

- API doc generation target added
- AssemblyInfo generation added (if ChangeLog.md exists)
- NUnit3 support added
- App icons added
- build workflow modeling and execution completely handled by FAKE
- Support for creation and publishing of NuGet packages
- Support for publishing releases to GitHub
