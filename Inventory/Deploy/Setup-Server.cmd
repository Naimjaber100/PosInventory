@echo off
rem Installs or upgrades the Pos database on this PC (the server). Asks for administrator rights.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Setup-Server.ps1" %*
pause
