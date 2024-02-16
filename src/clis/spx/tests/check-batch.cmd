@echo off
spx run --jobs test.batch.transcription.*.job
if %errorlevel%==0 echo BATCH-TESTS SUCCEEDED
if not %errorlevel%==0 echo BATCH-TESTS FAILED
