@echo off

echo.
echo Checking @key and @region ...

vz config @key | findstr /i not >nul
if not %errorlevel%==1 echo.  NOT FOUND... TRY: vz config --set key YOUR-KEY
if not %errorlevel%==1 goto :EOF

vz config @region | findstr /i not >nul
if not %errorlevel%==1 echo.  NOT FOUND... TRY: vz config --set region YOUR-REGION
if not %errorlevel%==1 goto :EOF

echo Checking @key and @region ... Done!
echo.

call check-cli.cmd
if not %errorlevel%==0 goto :EOF
