@echo off
setlocal

cd ..

dotnet publish

del /S /Q /F \bin\Plainion.CI\*

xcopy /E /Y \Workspace\Plainion\Plainion.CI\bin\Debug\publish \bin\Plainion.CI

 

endlocal