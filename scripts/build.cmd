@echo off
setlocal

REM Use x.y.z for version. MSI ignores the fourth product version field.
set VERSION=1.0.0
set PLATFORM=x64
set AZURE_CLI_VERSION=2.56.0
set DOTNET_VERSION=7.0.15
set DOTNET_URL=https://download.visualstudio.microsoft.com/download/pr/83b58670-c0ea-4442-ad35-beb5009f5396/bcf1e10f57dbeea2f46490430abf6882/dotnet-runtime-%DOTNET_VERSION%-win-%PLATFORM%.exe
set DOTNET_SIZE=28240232
set DOTNET_SHA1=4aa83e1a1d2cde8e47d7715269b7d53430e8babd

where candle.exe >nul
if %ERRORLEVEL% neq 0 set PATH=%PATH%;C:\Program Files (x86)\WiX Toolset v3.14\bin
where candle.exe >nul
if %ERRORLEVEL% neq 0 (
  echo Error: Install WiX v3.14 Toolset from https://wixtoolset.org/docs/v3/releases/v3-14-0-6526/ 1>&2
  exit /b 1
)

candle.exe Azure-AI-CLI.wxs -dversion=%VERSION% -dplatform=%PLATFORM%
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

light.exe Azure-AI-CLI-Bundle.wixobj -ext WixBalExtension -ext WixUtilExtension -o Azure-AI-CLI-Setup.exe
if %ERRORLEVEL% neq 0 (
  set EXITCODE=%ERRORLEVEL%
  echo Error from light.exe [%EXITCODE%] 1>&2
  exit /b %EXITCODE%
)

:end
endlocal

echo Run Azure-AI-CLI-Setup.exe to install.