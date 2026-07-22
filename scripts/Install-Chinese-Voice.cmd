@echo off
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-Chinese-Voice.ps1"
if errorlevel 1 pause
