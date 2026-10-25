@echo off
setlocal
rem Argument 1: PID of current process
rem Argument 2: Path of new downloaded executable
rem Argument 3: Target executable path to replace and launch

set TARGET_PID=%~1
set NEW_EXE=%~2
set TARGET_EXE=%~3

echo [Updater] Waiting for receiver process %TARGET_PID% to exit...
timeout /t 2 /nobreak >nul 2>&1
taskkill /PID %TARGET_PID% /F >nul 2>&1

echo [Updater] Replacing binary %TARGET_EXE% with %NEW_EXE%...
copy /Y "%NEW_EXE%" "%TARGET_EXE%" >nul 2>&1
del "%NEW_EXE%" >nul 2>&1

echo [Updater] Starting updated receiver...
start "" "%TARGET_EXE%"
exit /b 0
