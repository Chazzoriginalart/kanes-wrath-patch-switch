@echo off
setlocal
set "GAME=%~1"
if not defined GAME set /p "GAME=Kane's Wrath game folder (contains CNC3EP1.exe): "
if not defined GAME exit /b 1
set "MAPS=%~2"
if not defined MAPS set "MAPS=%APPDATA%\Command & Conquer 3 Kane's Wrath\Maps"
"%~dp0ReleaseInstall.exe" "%~dp0Payload" "%GAME%" "%MAPS%" "%TEMP%\KaneWrathPatchSwitch-R25j-R3-result.txt"
set "RESULT=%ERRORLEVEL%"
if exist "%TEMP%\KaneWrathPatchSwitch-R25j-R3-result.txt" type "%TEMP%\KaneWrathPatchSwitch-R25j-R3-result.txt"
if "%~1"=="" pause
exit /b %RESULT%
