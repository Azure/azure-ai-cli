@echo off

find cli|grep \.test$>test.cli.files
vz run --jobs @test.cli.files

if %errorlevel%==0 echo CLI-TESTS SUCCEEDED
if not %errorlevel%==0 echo CLI-TESTS FAILED
