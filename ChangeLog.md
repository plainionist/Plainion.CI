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
