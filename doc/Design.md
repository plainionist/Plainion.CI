# Design Overview

Plainion.CI uses [FAKE](https://fsharp.github.io/FAKE/) under the hood for modelling and executing the build workflow.

The build definition specified in the UI is saved into two xml files (.gc and .gc.<user>) in your project root.

These files are then read by the FAKE script(s) and used to model and configure the build workflow.

# Guidelines

Modules are prefixed with "P" in order to avoid naming conflicts with FAKE modules of same name, e.g.: "PNuGet" instead of "NuGet".
