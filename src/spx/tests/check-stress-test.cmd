@echo off
spx @test.stress
if %errorlevel%==0 echo STRESS-TESTS SUCCEEDED
if not %errorlevel%==0 echo STRESS-TESTS FAILED
