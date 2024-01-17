@echo off
dotnet.exe tool uninstall --global Azure.AI.CLI >"%TEMP%\UninstallCLI.log" 2>&1