@echo off
dotnet.exe tool uninstall --global Azure.AI.CLI
if %ERRORLEVEL% neq 0 pause
