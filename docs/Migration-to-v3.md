
# How to migrate from v2 to v3?

## References to external libraries

Replace

```
#r "/bin/Plainion.CI/FAKE/FakeLib.dll"
#load "/bin/Plainion.CI/bits/PlainionCI.fsx"
```

with 

```
#r "/bin/Plainion.CI/Fake.Core.Target.dll"
#r "/bin/Plainion.CI/Fake.IO.FileSystem.dll"
#r "/bin/Plainion.CI/Fake.IO.Zip.dll"
#r "/bin/Plainion.CI/Plainion.CI.Tasks.dll"
```

and add all references you need.
You can add further references with "#r packet:" syntax. Checkout Paket and FAKE documentation for further details.

## Passing targets to FAKE scripts

Instead of just passing the target name you now need to use "--target (target name)".
