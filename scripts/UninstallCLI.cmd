@echo off
dotnet.exe tool uninstall --global Azure.AI.CLI
REM Skip pause with a non-empty argument
if "%~1"=="" pause
