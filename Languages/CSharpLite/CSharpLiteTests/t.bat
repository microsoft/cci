@echo off
test.exe
if errorlevel 1 echo test failed
peverify test.exe