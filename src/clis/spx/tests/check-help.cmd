@echo off
setlocal
set _publicCommands=init;config;recognize;synthesize;intent;translate;batch;csr;profile;speaker;webjob
set _publicCommandsSorted=batch;config;csr;init;intent;profile;recognize;speaker;synthesize;translate;webjob
set _doesntExist=foobar doesn't exist

spx >nul
if %errorlevel%==0 spx run --spx --expect "%_publicCommands%" --args ""
if %errorlevel%==0 spx run --spx --expect "%_publicCommands%" --args "help"
if %errorlevel%==0 spx run --spx --not expect "(?i)error parsing" --foreach command in "%_publicCommands%;run"
if %errorlevel%==0 spx run --spx --not expect "(?i)cannot find" --command "help" --foreach item in "%_publicCommands%;run"
if %errorlevel%==0 spx quiet run --spx --expect "%_publicCommandsSorted%" --args "quiet help list"
if %errorlevel%==0 spx quiet run --spx --expect "%_publicCommandsSorted%" --args "quiet help list --expand"
if %errorlevel%==0 spx quiet run --spx --expect "%_publicCommandsSorted%" --args "quiet help list"| spx run --foreach command in -
if %errorlevel%==0 spx run --command "%_doesntExist%" --expect "(?i)error parsing"
if %errorlevel%==0 spx run --command "help %_doesntExist% --interactive false" --expect "(?i)cannot.*%_doesntExist%"

if %errorlevel%==0 echo CLI-HELP SUCCEEDED
if not %errorlevel%==0 echo CLI-HELP FAILED
