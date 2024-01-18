@echo off
setlocal

set CLI_NUPKG_URL_PREFIX=https://csspeechstorage.blob.core.windows.net/drop/private/ai

REM Azure AI CLI version e.g. 1.0.0 or 1.0.0-preview-20231214.1
if "%~1"=="" (
  echo Error: Azure AI CLI version number is not set. 1>&2
  goto end
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
  echo Error: curl.exe was not found. 1>&2
  goto end
)

REM Dotnet produces an error if there are invalid .nupkgs in the source path.
del /f /q "%CLI_NUPKG_LOCAL_FOLDER%\Azure.AI.CLI*.nupkg" 2>nul

REM Download Azure AI CLI .nupkg
curl.exe --output "%CLI_NUPKG_LOCAL_FOLDER%\%CLI_NUPKG_FILE%" --url %CLI_NUPKG_URL_PREFIX%/%CLI_NUPKG_FILE%
if %ERRORLEVEL% neq 0 (
  echo Error: Failed to download Azure AI CLI. 1>&2
  goto end
)

REM Install Azure AI CLI
set DOTNET_NOLOGO=true
set DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true
dotnet tool update --global --add-source "%CLI_NUPKG_LOCAL_FOLDER%" Azure.AI.CLI --verbosity normal --version [%CLI_VERSION%]
if %ERRORLEVEL% neq 0 (
  echo Error: Failed to install Azure AI CLI. 1>&2
  goto end
)

:end
REM Skip pause with a non-empty second argument
if "%~2"=="" pause

endlocal
