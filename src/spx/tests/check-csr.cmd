@echo off
spx run --jobs test.csr.*.job
if %errorlevel%==0 echo CSR-TESTS SUCCEEDED
if not %errorlevel%==0 echo CSR-TESTS FAILED
