@echo off
setlocal

REM Detach/attach the bundle bootstrap engine so that it can be properly signed together with the bundle exe.
REM ref. https://wixtoolset.org/docs/v3/overview/insignia/

if "%~1"=="" (
  echo Error: Action is not specified. 1>&2
  exit /b 1
)

if "%~2"=="" (
  echo Error: Target platform is not set. 1>&2
  exit /b 2
)

if "%~3"=="" (
  echo Error: Package version is not set. 1>&2
  exit /b 3
)

set ACTION=%~1
set TARGET_PLATFORM=%~2
set PACKAGE_VERSION=%~3

set UNSIGNED_BUNDLE=Setup-%TARGET_PLATFORM%.exe
set BUNDLE_ENGINE=engine-%TARGET_PLATFORM%.exe
set SIGNED_BUNDLE=Azure-AI-CLI-Setup-%PACKAGE_VERSION%-%TARGET_PLATFORM%.exe

REM Check for WiX toolset.
where insignia.exe >nul 2>&1
if %ERRORLEVEL% neq 0 set PATH=%PATH%;C:\Program Files (x86)\WiX Toolset v3.11\bin;C:\Program Files (x86)\WiX Toolset v3.14\bin
where insignia.exe >nul 2>&1
if %ERRORLEVEL% neq 0 (
  echo Error: Install WiX v3.14 Toolset from https://wixtoolset.org/docs/v3/releases/v3-14-0-6526/ 1>&2
  exit /b 4
)

if not exist %UNSIGNED_BUNDLE% (
  echo Error: %UNSIGNED_BUNDLE% not found. 1>&2
  exit /b 5
)

REM Detach engine from the package bundle installer.
if "%ACTION%"=="detach" (
  insignia -ib %UNSIGNED_BUNDLE% -o %BUNDLE_ENGINE%
  if %ERRORLEVEL% neq 0 (
    set EXITCODE=%ERRORLEVEL%
    echo Error from insignia.exe while detaching engine [%EXITCODE%] 1>&2
    exit /b %EXITCODE%
  )
  echo Detached %BUNDLE_ENGINE% from %UNSIGNED_BUNDLE%
  goto end
)

REM engine.exe is expected to be signed between these detach and attach actions.

REM (Re)attach engine to the package bundle installer.
if "%ACTION%"=="attach" (
  if not exist %BUNDLE_ENGINE% (
    echo Error: %BUNDLE_ENGINE% not found. 1>&2
    exit /b 6
  )
  insignia -ab %BUNDLE_ENGINE% %UNSIGNED_BUNDLE% -o %SIGNED_BUNDLE%
  if %ERRORLEVEL% neq 0 (
    set EXITCODE=%ERRORLEVEL%
    echo Error from insignia.exe while attaching engine [%EXITCODE%] 1>&2
    exit /b %EXITCODE%
  )
  echo Attached %BUNDLE_ENGINE% to %SIGNED_BUNDLE%
  goto end
)

:end
endlocal
