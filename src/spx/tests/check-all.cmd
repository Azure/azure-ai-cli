@echo off

echo.
echo Checking @key and @region ...

spx config @key | findstr /i not >nul
if not %errorlevel%==1 echo.  NOT FOUND... TRY: spx config @key --set YOUR-KEY
if not %errorlevel%==1 goto :EOF

spx config @region | findstr /i not >nul
if not %errorlevel%==1 echo.  NOT FOUND... TRY: spx config @region --set YOUR-REGION
if not %errorlevel%==1 goto :EOF

echo Checking @key and @region ... Done!
echo.

call check-batch.cmd
if not %errorlevel%==0 goto :EOF

call check-csr.cmd
if not %errorlevel%==0 goto :EOF

call check-cli.cmd
if not %errorlevel%==0 goto :EOF
