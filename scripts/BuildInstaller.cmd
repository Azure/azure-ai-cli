@echo off
setlocal

REM Dependencies
set AZURE_CLI_VERSION=2.56.0
set DOTNET_VERSION=7.0.405
set DOTNET_URL=https://download.visualstudio.microsoft.com/download/pr/febc46ff-cc68-4bee-83d2-c34786b5ca68/524ef9b25d29dc90efdb0fba0f589779/dotnet-sdk-%DOTNET_VERSION%-win-%TARGET_PLATFORM%.exe
set DOTNET_SIZE=229741072
set DOTNET_SHA1=8d40790ae79bfc6f29f1ab280801e6c019ae5633

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
set INSTALLER_FILE=Azure-AI-CLI-Setup-%PACKAGE_VERSION%-%TARGET_PLATFORM%.exe

REM Check for WiX toolset
where candle.exe >nul 2>&1
if %ERRORLEVEL% neq 0 set PATH=%PATH%;C:\Program Files (x86)\WiX Toolset v3.11\bin;C:\Program Files (x86)\WiX Toolset v3.14\bin
where candle.exe >nul 2>&1
if %ERRORLEVEL% neq 0 (
  echo Error: Install WiX v3.14 Toolset from https://wixtoolset.org/docs/v3/releases/v3-14-0-6526/ 1>&2
  exit /b 1
)

REM Build AI CLI installer .msi
candle.exe Azure-AI-CLI.wxs -dproductVersion=%PRODUCT_VERSION% -dpackageVersion=%PACKAGE_VERSION% -dtargetPlatform=%TARGET_PLATFORM%
if %ERRORLEVEL% neq 0 (
  set EXITCODE=%ERRORLEVEL%
  echo Error from candle.exe [%EXITCODE%] 1>&2
  exit /b %EXITCODE%
)

light.exe Azure-AI-CLI.wixobj -ext WixUIExtension
if %ERRORLEVEL% neq 0 (
  set EXITCODE=%ERRORLEVEL%
  echo Error from light.exe [%EXITCODE%] 1>&2
  exit /b %EXITCODE%
)

REM Build installation bundle .exe
candle.exe Azure-AI-CLI-Bundle.wxs -ext WixBalExtension -ext WixUtilExtension ^
  -dproductVersion=%PRODUCT_VERSION% -dtargetPlatform=%TARGET_PLATFORM% -dazureCliVersion=%AZURE_CLI_VERSION% ^
  -ddotNetVersion=%DOTNET_VERSION% -ddotNetUrl=%DOTNET_URL% -ddotNetSize=%DOTNET_SIZE% -ddotNetSha1=%DOTNET_SHA1%
if %ERRORLEVEL% neq 0 (
  set EXITCODE=%ERRORLEVEL%
  echo Error from candle.exe [%EXITCODE%] 1>&2
  exit /b %EXITCODE%
)

light.exe Azure-AI-CLI-Bundle.wixobj -cultures:en-us -loc Azure-AI-CLI-Bundle.wxl -ext WixBalExtension -ext WixUtilExtension -out %INSTALLER_FILE%
if %ERRORLEVEL% neq 0 (
  set EXITCODE=%ERRORLEVEL%
  echo Error from light.exe [%EXITCODE%] 1>&2
  exit /b %EXITCODE%
)

:end
echo Run %INSTALLER_FILE% to install.
endlocal
