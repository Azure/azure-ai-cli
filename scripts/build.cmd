@echo off
setlocal

set PLATFORM=x64
set AZURE_CLI_VERSION=2.56.0
set DOTNET_VERSION=7.0.15
set DOTNET_URL=https://download.visualstudio.microsoft.com/download/pr/83b58670-c0ea-4442-ad35-beb5009f5396/bcf1e10f57dbeea2f46490430abf6882/dotnet-runtime-%DOTNET_VERSION%-win-%PLATFORM%.exe
set DOTNET_SIZE=28240232
set DOTNET_SHA1=4aa83e1a1d2cde8e47d7715269b7d53430e8babd

REM for testing with a non-VS-installed version
REM set DOTNET_VERSION=7.0.14
REM set DOTNET_URL=https://download.visualstudio.microsoft.com/download/pr/5e3be9c1-4b4c-4605-b3bc-18ef04b3c8d5/b1f864adc9c81ab6680385a4270b3887/dotnet-runtime-%DOTNET_VERSION%-win-%PLATFORM%.exe
REM set DOTNET_SIZE=28253944
REM set DOTNET_SHA1=99cee84493695e798fb031bb2eb757d6ae15b0b4

where candle.exe >nul
IF %ERRORLEVEL% NEQ 0 set PATH=%PATH%;C:\Program Files (x86)\WiX Toolset v3.14\bin
where candle.exe >nul
IF %ERRORLEVEL% NEQ 0 (
  echo Error: Install WiX v3.14 Toolset from https://wixtoolset.org/docs/v3/releases/v3-14-0-6526/ 1>&2
  exit /b 1
)

candle.exe Azure-AI-CLI.wxs -dplatform=%PLATFORM%
IF %ERRORLEVEL% NEQ 0 (
  set EXITCODE=%ERRORLEVEL%
  echo Error from candle.exe [%EXITCODE%] 1>&2
  exit /b %EXITCODE%
)

light.exe Azure-AI-CLI.wixobj -ext WixUIExtension
IF %ERRORLEVEL% NEQ 0 (
  set EXITCODE=%ERRORLEVEL%
  echo Error from light.exe [%EXITCODE%] 1>&2
  exit /b %EXITCODE%
)

candle.exe Azure-AI-CLI-Bundle.wxs -ext WixBalExtension -ext WixNetFxExtension -ext WixUtilExtension ^
  -dplatform=%PLATFORM% -dazureCliVersion=%AZURE_CLI_VERSION% -ddotNetVersion=%DOTNET_VERSION% ^
  -ddotNetUrl=%DOTNET_URL% -ddotNetSize=%DOTNET_SIZE% -ddotNetSha1=%DOTNET_SHA1%
IF %ERRORLEVEL% NEQ 0 (
  set EXITCODE=%ERRORLEVEL%
  echo Error from candle.exe [%EXITCODE%] 1>&2
  exit /b %EXITCODE%
)

light.exe Azure-AI-CLI-Bundle.wixobj -ext WixBalExtension -ext WixNetFxExtension -ext WixUtilExtension -o Azure-AI-CLI-Setup.exe
IF %ERRORLEVEL% NEQ 0 (
  set EXITCODE=%ERRORLEVEL%
  echo Error from light.exe [%EXITCODE%] 1>&2
  exit /b %EXITCODE%
)

:end
endlocal

echo Run Azure-AI-CLI-Setup.exe to install.