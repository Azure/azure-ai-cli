@echo off
setlocal

REM Azure AI CLI package version (e.g. 1.0.0 or 1.0.0-preview-20231214.1)
if "%~1"=="" (
  echo Error: Azure AI CLI package version is not set. 1>&2
  exit /b 1
)

REM Azure AI CLI product version x.y.z (e.g. 1.0.0), ref. https://learn.microsoft.com/windows/win32/msi/productversion
REM This is the version shown in the list of installed programs.
REM If the package version changes but product version remains the same,
REM an existing installation cannot be upgraded without uninstalling it first!
if "%~2"=="" (
  echo Error: Azure AI CLI product version is not set. 1>&2
  exit /b 2
)

set PACKAGE_VERSION=%~1
set PRODUCT_VERSION=%~2
set TARGET_PLATFORM=x64
set INSTALLER_FILE=Setup-%TARGET_PLATFORM%.exe
set PACKAGE_URL=https://csspeechstorage.blob.core.windows.net/drop/private/ai/Azure.AI.CLI.%PACKAGE_VERSION%.nupkg

REM Dependencies
set AZURE_CLI_VERSION=2.57.0
set AZURE_CLI_INSTALLER=azure-cli-%AZURE_CLI_VERSION%-%TARGET_PLATFORM%.msi
set AZURE_CLI_URL=https://azcliprod.blob.core.windows.net/msi/%AZURE_CLI_INSTALLER%
set DOTNET_VERSION=8.0.200
set DOTNET_INSTALLER=dotnet-sdk-%DOTNET_VERSION%-win-%TARGET_PLATFORM%.exe
set DOTNET_URL=https://dotnetcli.azureedge.net/dotnet/Sdk/%DOTNET_VERSION%/%DOTNET_INSTALLER%

REM Check for WiX toolset
where candle.exe >nul 2>&1
if %ERRORLEVEL% neq 0 set PATH=%PATH%;C:\Program Files (x86)\WiX Toolset v3.11\bin;C:\Program Files (x86)\WiX Toolset v3.14\bin
where candle.exe >nul 2>&1
if %ERRORLEVEL% neq 0 (
  echo Error: Install WiX v3.14 Toolset from https://wixtoolset.org/docs/v3/releases/v3-14-0-6526/ 1>&2
  exit /b 3
)

REM Check for curl.exe (https://techcommunity.microsoft.com/t5/containers/tar-and-curl-come-to-windows/ba-p/382409)
where curl.exe >nul 2>&1
if %ERRORLEVEL% neq 0 (
  echo Error: curl.exe not found 1>&2
  exit /b 4
)

REM Download Azure CLI installer
curl.exe --output %AZURE_CLI_INSTALLER% --silent --url %AZURE_CLI_URL%
if %ERRORLEVEL% neq 0 (
  echo Error while downloading Azure CLI installer 1>&2
  exit /b 5
)

REM Download .NET SDK installer
curl.exe --output %DOTNET_INSTALLER% --silent --url %DOTNET_URL%
if %ERRORLEVEL% neq 0 (
  echo Error while downloading .NET SDK installer 1>&2
  exit /b 6
)

REM Build AI CLI installer .msi
candle.exe Azure-AI-CLI.wxs -dproductVersion=%PRODUCT_VERSION% -dpackageVersion=%PACKAGE_VERSION% -dpackageUrl=%PACKAGE_URL% -dtargetPlatform=%TARGET_PLATFORM%
if %ERRORLEVEL% neq 0 (
  echo Error from candle.exe 1>&2
  exit /b 7
)

light.exe Azure-AI-CLI.wixobj -ext WixUIExtension -ext WixUtilExtension
if %ERRORLEVEL% neq 0 (
  echo Error from light.exe 1>&2
  exit /b 8
)

REM Build installation bundle .exe
candle.exe Azure-AI-CLI-Setup.wxs -ext WixBalExtension -ext WixUtilExtension ^
  -dproductVersion=%PRODUCT_VERSION% -dtargetPlatform=%TARGET_PLATFORM% ^
  -dazureCliVersion=%AZURE_CLI_VERSION% -dazureCliUrl=%AZURE_CLI_URL% ^
  -ddotNetVersion=%DOTNET_VERSION% -ddotNetUrl=%DOTNET_URL%
if %ERRORLEVEL% neq 0 (
  echo Error from candle.exe 1>&2
  exit /b 9
)

light.exe Azure-AI-CLI-Setup.wixobj -ext WixBalExtension -ext WixUtilExtension -out %INSTALLER_FILE%
if %ERRORLEVEL% neq 0 (
  echo Error from light.exe 1>&2
  exit /b 10
)

:end
echo Built %INSTALLER_FILE% successfully!
endlocal
