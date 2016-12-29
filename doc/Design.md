# Design Overview

Plainion.CI uses [FAKE](https://fsharp.github.io/FAKE/) under the hood for modelling and executing the build workflow.

The build definition specified in the UI is saved into two xml files (.gc and .gc.<user>) in your project root.

These files are then read by the FAKE script(s) and used to model and configure the build workflow.
