@echo off
setlocal

set CLI_MSI_URL_PREFIX=https://azcliprod.blob.core.windows.net/msi

REM Azure CLI version
if "%~1"=="" (
  echo Error: Missing parameter for Azure CLI version number, e.g. "2.56.0" 1>&2
  exit /b 1
) else (
  set CLI_MSI_VERSION=%~1
)

REM Azure CLI platform
if "%~2"=="" (
  echo Error: Missing parameter for Azure CLI platform, "x64" for 64-bit or "x86" for 32-bit 1>&2
  exit /b 2
) else if "%~2"=="x64" (
  set CLI_INSTALLER_URL=%CLI_MSI_URL_PREFIX%/azure-cli-%CLI_MSI_VERSION%-x64.msi
) else if "%~2"=="x86" (
  set CLI_INSTALLER_URL=%CLI_MSI_URL_PREFIX%/azure-cli-%CLI_MSI_VERSION%.msi
) else (
  echo Error: Unsupported Azure CLI platform "%~2" 1>&2
  exit /b 3
)

REM Local Azure CLI installer path
if exist "%TEMP%"\ (
  set CLI_MSI_LOCAL_PATH=%TEMP%\azure-cli.msi
) else (
  set CLI_MSI_LOCAL_PATH=azure-cli.msi
)

REM Windows curl.exe (https://techcommunity.microsoft.com/t5/containers/tar-and-curl-come-to-windows/ba-p/382409)
where curl.exe >nul
if %ERRORLEVEL% neq 0 (
  echo Error: curl.exe not found 1>&2
  exit /b 4
)

REM Download Azure CLI installer
curl.exe --output "%CLI_MSI_LOCAL_PATH%" --silent --url %CLI_INSTALLER_URL%
if %ERRORLEVEL% neq 0 (
  echo Error while downloading Azure CLI installer 1>&2
  exit /b 5
)

REM Install Azure CLI
start /wait msiexec /i "%CLI_MSI_LOCAL_PATH%" /passive /norestart
if %ERRORLEVEL% neq 0 (
  echo Error while installing Azure CLI 1>&2
  exit /b 6
)

REM Clean up
del /f /q "%CLI_MSI_LOCAL_PATH%" 2>nul

:end
endlocal