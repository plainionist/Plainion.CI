---
title: Examples
navigation_weight: 10
---

# Examples

Here are some examples how I use Plainion.CI to automate my deployments.

## Plainion.Windows - Release a library on NuGet

[Plainion.Windows](https://github.com/plainionist/Plainion.Windows) is a .NET library extending WPF for simplicity and completeness.
It provides API documentation and is available on [NuGet](https://www.nuget.org/packages/Plainion.Windows).

Configuration

- [Build Definition](https://github.com/plainionist/Plainion.Windows/blob/master/Plainion.Windows.gc)
- [Deployment Targets](https://github.com/plainionist/Plainion.Windows/blob/master/build/Targets.fsx)
- [NuGet Spec](https://github.com/plainionist/Plainion.Windows/blob/master/build/Plainion.Windows.nuspec)


## Plainion.GraphViz - Release an app on NuGet

[Plainion.GraphViz](http://plainionist.github.io/Plainion.GraphViz/) is the tamer of complex graphs and even galaxies.
It is released on GitHub.

Configuration

- [Build Definition](https://github.com/plainionist/Plainion.GraphViz/blob/master/Plainion.GraphViz.gc)
- [Deployment Targets](https://github.com/plainionist/Plainion.GraphViz/blob/master/build/Targets.fsx)

## My Blog - Publishing to GitHub Pages

I even release my blog with Plainion.CI ;-)
It is hosted on GitHub Pages.

Configuration

- [Build Definition](https://github.com/plainionist/plainionist.github.io/blob/master/plainionist.github.io.gc)
  - Hint: to make this work I created a "dummy" Visual Studio project which contains all the blog files and a solution file
    which is used by Plainion.CI.



