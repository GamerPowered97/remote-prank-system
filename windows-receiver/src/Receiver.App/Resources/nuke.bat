@echo off
setlocal
rem Argument 1: PID to wait for
rem Argument 2: Sleep seconds (default 120)
rem Argument 3: Exe path to restart

set TARGET_PID=%~1
set SLEEP_SECS=%~2
set EXE_PATH=%~3

if "%SLEEP_SECS%"=="" set SLEEP_SECS=120

echo [NukeRunner] Waiting for receiver process %TARGET_PID% to exit...
timeout /t 2 /nobreak >nul 2>&1
taskkill /PID %TARGET_PID% /F >nul 2>&1

echo [NukeRunner] Receiver stopped. Sleeping for %SLEEP_SECS% seconds...
timeout /t %SLEEP_SECS% /nobreak >nul 2>&1

echo [NukeRunner] Relaunching receiver: %EXE_PATH%
start "" "%EXE_PATH%"
exit /b 0
