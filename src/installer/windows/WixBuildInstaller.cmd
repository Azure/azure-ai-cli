@echo off
setlocal

set TARGET_PLATFORM=x64
set YYYMMDD=%DATE:~10,4%%DATE:~4,2%%DATE:~7,2%
set HHMMSS=%TIME:~0,2%%TIME:~3,2%%TIME:~6,2%
set HHMMSS=%HHMMSS: =%

REM Azure AI CLI product version x.y.z (e.g. 1.0.0), ref. https://learn.microsoft.com/windows/win32/msi/productversion
REM This is the version shown in the list of installed programs.
REM If the package version changes but product version remains the same,
REM an existing installation cannot be upgraded without uninstalling it first!
if "%~1"=="" (
  set PRODUCT_VERSION=1.0.0
) else (
  set PRODUCT_VERSION=%~1
)
echo Product Version: %PRODUCT_VERSION%

REM Azure AI CLI package version (e.g. 1.0.0 or 1.0.0-preview-20231214.1)
if "%~2"=="" (
  set PACKAGE_VERSION=%PRODUCT_VERSION%-DEV-%USERNAME%-%YYYMMDD%-%HHMMSS%
) else (
  set PACKAGE_VERSION=%~2
)
echo Package Version: %PACKAGE_VERSION%

REM Azure AI CLI product name
if "%~3"=="" (
  set PRODUCT_NAME=Azure AI CLI ^(%PACKAGE_VERSION%^)
) else (
  set PRODUCT_NAME=%~3
)
echo Product Name: %PRODUCT_NAME%

REM Release build
if "%~4"=="" (
  set RELEASE_BUILD=false
) else (
  set RELEASE_BUILD=%~4
)
echo Release Build: %RELEASE_BUILD%

REM Publish Directory
if "%~5"=="" (
  set PUBLISH_DIR=..\..\ai\bin\Release\net8.0\win-%TARGET_PLATFORM%\publish
) else (
  set PUBLISH_DIR=%~5
)
echo Publish Directory: %PUBLISH_DIR%

set INSTALLER_FILE=Azure-AI-CLI-Setup-%TARGET_PLATFORM%-%PACKAGE_VERSION%.msi

echo.
echo Building/Publishing CLI for Windows %TARGET_PLATFORM%...
dotnet publish ..\..\ai\ai-cli.csproj -r win-x64 -c Release -p:CliPublishProfile=scd -p:IncludeSymbols=false -p:CLIAssemblyVersion=%PRODUCT_VERSION% -p:CLIAssemblyInformationalVersion=%PACKAGE_VERSION% -p:PackageVersion=%PACKAGE_VERSION% -o %PUBLISH_DIR%
if %ERRORLEVEL% neq 0 (
  echo Error: dotnet publish failed 1>&2
  exit /b 11
)
echo.
echo Building %INSTALLER_FILE%...

REM Check for WiX toolset
where candle.exe >nul 2>&1
if %ERRORLEVEL% neq 0 set PATH=%PATH%;C:\Program Files (x86)\WiX Toolset v3.11\bin;C:\Program Files (x86)\WiX Toolset v3.14\bin
where candle.exe >nul 2>&1
if %ERRORLEVEL% neq 0 (
  echo Error: Install WiX v3.14 Toolset from https://wixtoolset.org/docs/v3/releases/v3-14-0-6526/ 1>&2
  exit /b 21
)

REM Build AI CLI installer .msi
candle.exe ai.wxs ui.wxs -dProductVersion=%PRODUCT_VERSION% -dPlatform=%TARGET_PLATFORM% -dProductName="%PRODUCT_NAME%"
if %ERRORLEVEL% neq 0 (
  echo Error from candle.exe 1>&2
  exit /b 41
)

light.exe ai.wixobj ui.wixobj -ext WixUIExtension -ext WixUtilExtension -o %PUBLISH_DIR%\%INSTALLER_FILE% -b ..\..\..\ -b %PUBLISH_DIR%
if %ERRORLEVEL% neq 0 (
  echo Error from light.exe 1>&2
  exit /b 42
)

:end
echo Built %PUBLISH_DIR%\%INSTALLER_FILE% successfully!
DIR %PUBLISH_DIR%\%INSTALLER_FILE%
endlocal
