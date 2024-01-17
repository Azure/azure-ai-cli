@echo off
setlocal

set CLI_NUPKG_URL_PREFIX=https://csspeechstorage.blob.core.windows.net/drop/private/ai

REM Azure CLI version
if "%~1"=="" (
  echo Error: Missing parameter for Azure AI CLI version number, e.g. "1.0.0" or "1.0.0-preview-20231214.1" 1>&2
  exit /b 1
) else (
  set CLI_VERSION=%~1
)

set CLI_NUPKG_FILE=Azure.AI.CLI.%CLI_VERSION%.nupkg

REM Local Azure AI CLI .nupkg folder
if exist "%TEMP%"\ (
  set CLI_NUPKG_LOCAL_FOLDER=%TEMP%
) else (
  set CLI_NUPKG_LOCAL_FOLDER=.
)

REM Windows curl.exe (https://techcommunity.microsoft.com/t5/containers/tar-and-curl-come-to-windows/ba-p/382409)
where curl.exe >nul
if %ERRORLEVEL% neq 0 (
  echo Error: curl.exe not found 1>&2
  exit /b 2
)

REM Dotnet produces an error if there are invalid .nupkgs in the source path.
del /f /q "%CLI_NUPKG_LOCAL_FOLDER%\Azure.AI.CLI*.nupkg" 2>nul

REM Download Azure AI CLI .nupkg
curl.exe --output "%CLI_NUPKG_LOCAL_FOLDER%\%CLI_NUPKG_FILE%" --silent --url %CLI_NUPKG_URL_PREFIX%/%CLI_NUPKG_FILE%
if %ERRORLEVEL% neq 0 (
  echo Error while downloading Azure AI CLI .nupkg 1>&2
  exit /b 3
)

REM Install Azure CLI
dotnet tool update --global --add-source "%CLI_NUPKG_LOCAL_FOLDER%" Azure.AI.CLI --version [%CLI_VERSION%]
if %ERRORLEVEL% neq 0 (
  echo Error while installing Azure AI CLI 1>&2
  exit /b 4
)

REM Clean up
del /f /q "%CLI_NUPKG_LOCAL_FOLDER%\%CLI_NUPKG_FILE%" 2>nul

:end
endlocal
