@echo off
cd /d "%~dp0scripts"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\start-frontend-dev.ps1"
if errorlevel 1 pause
