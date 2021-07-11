
# Troubleshooting

## Unsupported log file format. Latest supported version is X, the log file has version Y.

- see https://github.com/fsharp/FAKE/issues/2515
- solution: keep direct reference in Plainion.CI.Tasks to MsBuild.StructuredLogger and update the version to latest available

## Octokit.NotFoundException: Not Found

- Means: GitHub WebAPI returned HTTP 404 
- Reason: 
