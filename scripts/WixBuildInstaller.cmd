@echo off
setlocal

REM Azure AI CLI package version (e.g. 1.0.0 or 1.0.0-preview-20231214.1)
if "%~1"=="" (
  echo Error: Azure AI CLI package version is not set. 1>&2
  exit /b 11
)

REM Azure AI CLI product version x.y.z (e.g. 1.0.0), ref. https://learn.microsoft.com/windows/win32/msi/productversion
REM This is the version shown in the list of installed programs.
REM If the package version changes but product version remains the same,
REM an existing installation cannot be upgraded without uninstalling it first!
if "%~2"=="" (
  echo Error: Azure AI CLI product version is not set. 1>&2
  exit /b 12
)

set PACKAGE_VERSION=%~1
set PRODUCT_VERSION=%~2
set TARGET_PLATFORM=x64
set INSTALLER_FILE=Setup-%TARGET_PLATFORM%.exe
set PACKAGE_URL=https://csspeechstorage.blob.core.windows.net/drop/private/ai/Azure.AI.CLI.%PACKAGE_VERSION%.nupkg

REM Dependencies (note: do NOT use redirecting URLs like aka.ms)
set AZURE_CLI_VERSION=2.58.0
set AZURE_CLI_INSTALLER=azure-cli-%AZURE_CLI_VERSION%-%TARGET_PLATFORM%.msi
set AZURE_CLI_URL=https://azcliprod.blob.core.windows.net/msi/%AZURE_CLI_INSTALLER%
set DOTNET_VERSION=8.0.202
set DOTNET_INSTALLER=dotnet-sdk-%DOTNET_VERSION%-win-%TARGET_PLATFORM%.exe
set DOTNET_URL=https://dotnetcli.azureedge.net/dotnet/Sdk/%DOTNET_VERSION%/%DOTNET_INSTALLER%
set VCRT_VERSION=14.38.33135
set VCRT_INSTALLER=VC_redist.%TARGET_PLATFORM%.exe
REM Below URL as redirected from https://aka.ms/vs/17/release/vc_redist.x64.exe while 14.38.33135 is the latest version.
set VCRT_URL=https://download.visualstudio.microsoft.com/download/pr/c7707d68-d6ce-4479-973e-e2a3dc4341fe/1AD7988C17663CC742B01BEF1A6DF2ED1741173009579AD50A94434E54F56073/%VCRT_INSTALLER%

REM Check for WiX toolset
where candle.exe >nul 2>&1
if %ERRORLEVEL% neq 0 set PATH=%PATH%;C:\Program Files (x86)\WiX Toolset v3.11\bin;C:\Program Files (x86)\WiX Toolset v3.14\bin
where candle.exe >nul 2>&1
if %ERRORLEVEL% neq 0 (
  echo Error: Install WiX v3.14 Toolset from https://wixtoolset.org/docs/v3/releases/v3-14-0-6526/ 1>&2
  exit /b 21
)

REM Check for curl.exe (https://techcommunity.microsoft.com/t5/containers/tar-and-curl-come-to-windows/ba-p/382409)
where curl.exe >nul 2>&1
if %ERRORLEVEL% neq 0 (
  echo Error: curl.exe not found 1>&2
  exit /b 22
)

REM Download Azure CLI installer
curl.exe --output %AZURE_CLI_INSTALLER% --silent --location --url %AZURE_CLI_URL%
if %ERRORLEVEL% neq 0 (
  echo Error while downloading Azure CLI installer 1>&2
  exit /b 31
)

REM Download .NET SDK installer
curl.exe --output %DOTNET_INSTALLER% --silent --location --url %DOTNET_URL%
if %ERRORLEVEL% neq 0 (
  echo Error while downloading .NET SDK installer 1>&2
  exit /b 32
)

REM Download Visual C++ runtime installer
curl.exe --output %VCRT_INSTALLER% --silent --location --url %VCRT_URL%
if %ERRORLEVEL% neq 0 (
  echo Error while downloading Visual C++ runtime installer 1>&2
  exit /b 33
)

REM Build AI CLI installer .msi
candle.exe Azure-AI-CLI.wxs -dproductVersion=%PRODUCT_VERSION% -dpackageVersion=%PACKAGE_VERSION% -dpackageUrl=%PACKAGE_URL% -dtargetPlatform=%TARGET_PLATFORM%
if %ERRORLEVEL% neq 0 (
  echo Error from candle.exe 1>&2
  exit /b 41
)

light.exe Azure-AI-CLI.wixobj -ext WixUIExtension -ext WixUtilExtension
if %ERRORLEVEL% neq 0 (
  echo Error from light.exe 1>&2
  exit /b 42
)

REM Build installation bundle .exe
candle.exe Azure-AI-CLI-Setup.wxs -ext WixBalExtension -ext WixUtilExtension ^
  -dproductVersion=%PRODUCT_VERSION% -dtargetPlatform=%TARGET_PLATFORM% ^
  -dazureCliVersion=%AZURE_CLI_VERSION% -dazureCliUrl=%AZURE_CLI_URL% ^
  -ddotNetVersion=%DOTNET_VERSION% -ddotNetUrl=%DOTNET_URL% ^
  -dvcrtVersion=%VCRT_VERSION% -dvcrtUrl=%VCRT_URL%
if %ERRORLEVEL% neq 0 (
  echo Error from candle.exe 1>&2
  exit /b 51
)

light.exe Azure-AI-CLI-Setup.wixobj -ext WixBalExtension -ext WixUtilExtension -out %INSTALLER_FILE%
if %ERRORLEVEL% neq 0 (
  echo Error from light.exe 1>&2
  exit /b 52
)

:end
echo Built %INSTALLER_FILE% successfully!
endlocal
