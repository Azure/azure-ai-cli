@echo off
setlocal

REM Use x.y.z for version. MSI ignores the fourth product version field.
REM This is also shown as the product version in the list of installed programs.
set VERSION=1.0.0
REM The AI CLI package (nupkg) version. This cannot be used as a product version.
set PACKAGE_VERSION=1.0.0-preview-20231214.1

set PLATFORM=x64
set INSTALLER=Azure-AI-CLI-Setup-%PACKAGE_VERSION%-%PLATFORM%.exe

REM Dependencies
set AZURE_CLI_VERSION=2.56.0
set DOTNET_VERSION=7.0.405
set DOTNET_URL=https://download.visualstudio.microsoft.com/download/pr/febc46ff-cc68-4bee-83d2-c34786b5ca68/524ef9b25d29dc90efdb0fba0f589779/dotnet-sdk-%DOTNET_VERSION%-win-%PLATFORM%.exe
set DOTNET_SIZE=229741072
set DOTNET_SHA1=8d40790ae79bfc6f29f1ab280801e6c019ae5633

where candle.exe >nul
if %ERRORLEVEL% neq 0 set PATH=%PATH%;C:\Program Files (x86)\WiX Toolset v3.14\bin
where candle.exe >nul
if %ERRORLEVEL% neq 0 (
  echo Error: Install WiX v3.14 Toolset from https://wixtoolset.org/docs/v3/releases/v3-14-0-6526/ 1>&2
  exit /b 1
)

candle.exe Azure-AI-CLI.wxs -dversion=%VERSION% -dpackageVersion=%PACKAGE_VERSION% -dplatform=%PLATFORM%
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

candle.exe Azure-AI-CLI-Bundle.wxs -ext WixBalExtension -ext WixUtilExtension ^
  -dversion=%VERSION% -dplatform=%PLATFORM% -dazureCliVersion=%AZURE_CLI_VERSION% ^
  -ddotNetVersion=%DOTNET_VERSION% -ddotNetUrl=%DOTNET_URL% -ddotNetSize=%DOTNET_SIZE% -ddotNetSha1=%DOTNET_SHA1%
if %ERRORLEVEL% neq 0 (
  set EXITCODE=%ERRORLEVEL%
  echo Error from candle.exe [%EXITCODE%] 1>&2
  exit /b %EXITCODE%
)

light.exe Azure-AI-CLI-Bundle.wixobj -ext WixBalExtension -ext WixUtilExtension -o %INSTALLER%
if %ERRORLEVEL% neq 0 (
  set EXITCODE=%ERRORLEVEL%
  echo Error from light.exe [%EXITCODE%] 1>&2
  exit /b %EXITCODE%
)

:end
echo Run %INSTALLER% to install.
endlocal
