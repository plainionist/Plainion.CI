## 2.0 - 2019-03-03

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
