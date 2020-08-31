@echo off
setlocal

del /S /Q /F .\bin\Debug\*

dotnet publish

bin\Debug\publish\Plainion.CI.exe Plainion.CI.gc
 

endlocal