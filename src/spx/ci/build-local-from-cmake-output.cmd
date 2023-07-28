@echo off

set SPEECHSDK_VERSION=1.22.0-alpha.0.1
set CLI_ASSEMBLY_VERSION=1.22.0
set CLI_ASSEMBLY_INFORMATIONAL_VERSION=1.22.0-alpha.0.1
set CMAKE_RUNTIME_OUTPUT_DIRECTORY=%~dp0\..\..\..\..\..\build\bin
set BUILD_CONFIGURATION=Release
set PACK_OUTPUT_PATH=%CMAKE_RUNTIME_OUTPUT_DIRECTORY%\artifacts

if exist "spx-cli.csproj" goto :BUILD_ALL_AND_PACK
echo Run `%~n0` from `clis\source\cli\spx` directory
goto :eof

:BUILD_ALL_AND_PACK
dotnet restore "spx-cli.csproj" -p:Platform=x64 -p:SpeechSDKVersion=%SPEECHSDK_VERSION% -p:CLIAssemblyVersion=%CLI_ASSEMBLY_VERSION% -p:CLIAssemblyInformationalVersion=%CLI_ASSEMBLY_INFORMATIONAL_VERSION% -p:LocalBuildSDKBinPath=%CMAKE_RUNTIME_OUTPUT_DIRECTORY%
dotnet build   "spx-cli.csproj" -p:Platform=x64 -p:SpeechSDKVersion=%SPEECHSDK_VERSION% -p:CLIAssemblyVersion=%CLI_ASSEMBLY_VERSION% -p:CLIAssemblyInformationalVersion=%CLI_ASSEMBLY_INFORMATIONAL_VERSION% -p:LocalBuildSDKBinPath=%CMAKE_RUNTIME_OUTPUT_DIRECTORY% -c %BUILD_CONFIGURATION% --no-restore 
dotnet pack    "spx-cli.csproj" -p:Platform=x64 -p:SpeechSDKVersion=%SPEECHSDK_VERSION% -p:CLIAssemblyVersion=%CLI_ASSEMBLY_VERSION% -p:CLIAssemblyInformationalVersion=%CLI_ASSEMBLY_INFORMATIONAL_VERSION% -p:LocalBuildSDKBinPath=%CMAKE_RUNTIME_OUTPUT_DIRECTORY% -c %BUILD_CONFIGURATION% --no-restore --no-build -p:IncludeSymbols=false -p:PackageVersion=%CLI_ASSEMBLY_INFORMATIONAL_VERSION% -o %PACK_OUTPUT_PATH%/nupkg

:BUILD_WIN
dotnet restore "spx-cli.csproj" -p:Platform=x64 -p:SpeechSDKVersion=%SPEECHSDK_VERSION% -p:CLIAssemblyVersion=%CLI_ASSEMBLY_VERSION% -p:CLIAssemblyInformationalVersion=%CLI_ASSEMBLY_INFORMATIONAL_VERSION% -p:LocalBuildSDKBinPath=%CMAKE_RUNTIME_OUTPUT_DIRECTORY% -r win-x64
dotnet build   "spx-cli.csproj" -p:Platform=x64 -p:SpeechSDKVersion=%SPEECHSDK_VERSION% -p:CLIAssemblyVersion=%CLI_ASSEMBLY_VERSION% -p:CLIAssemblyInformationalVersion=%CLI_ASSEMBLY_INFORMATIONAL_VERSION% -p:LocalBuildSDKBinPath=%CMAKE_RUNTIME_OUTPUT_DIRECTORY% -r win-x64 -c %BUILD_CONFIGURATION% --no-restore 
dotnet publish "spx-cli.csproj" -p:Platform=x64 -p:SpeechSDKVersion=%SPEECHSDK_VERSION% -p:CLIAssemblyVersion=%CLI_ASSEMBLY_VERSION% -p:CLIAssemblyInformationalVersion=%CLI_ASSEMBLY_INFORMATIONAL_VERSION% -p:LocalBuildSDKBinPath=%CMAKE_RUNTIME_OUTPUT_DIRECTORY% -r win-x64 -c %BUILD_CONFIGURATION% --no-restore --no-build -p:IncludeSymbols=false -p:PublishProfile="Properties\PublishProfiles\folder publish spx-cli release (win-x64).pubxml" 

:BUILD_LINUX
dotnet restore "spx-cli.csproj" -p:Platform=x64 -p:SpeechSDKVersion=%SPEECHSDK_VERSION% -p:CLIAssemblyVersion=%CLI_ASSEMBLY_VERSION% -p:CLIAssemblyInformationalVersion=%CLI_ASSEMBLY_INFORMATIONAL_VERSION% -p:LocalBuildSDKBinPath=%CMAKE_RUNTIME_OUTPUT_DIRECTORY% -r linux-x64
dotnet build   "spx-cli.csproj" -p:Platform=x64 -p:SpeechSDKVersion=%SPEECHSDK_VERSION% -p:CLIAssemblyVersion=%CLI_ASSEMBLY_VERSION% -p:CLIAssemblyInformationalVersion=%CLI_ASSEMBLY_INFORMATIONAL_VERSION% -p:LocalBuildSDKBinPath=%CMAKE_RUNTIME_OUTPUT_DIRECTORY% -r linux-x64 -c %BUILD_CONFIGURATION% --no-restore            -p:IncludeSymbols=false
dotnet publish "spx-cli.csproj" -p:Platform=x64 -p:SpeechSDKVersion=%SPEECHSDK_VERSION% -p:CLIAssemblyVersion=%CLI_ASSEMBLY_VERSION% -p:CLIAssemblyInformationalVersion=%CLI_ASSEMBLY_INFORMATIONAL_VERSION% -p:LocalBuildSDKBinPath=%CMAKE_RUNTIME_OUTPUT_DIRECTORY% -r linux-x64 -c %BUILD_CONFIGURATION% --no-restore --no-build -p:PublishProfile="Properties\PublishProfiles\folder publish spx-cli release (linux-x64).pubxml" 

dir %PACK_OUTPUT_PATH%\*.nupkg /s

goto :eof
