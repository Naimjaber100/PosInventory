@echo off
rem Connects this cashier PC to the server: Setup-Station.cmd -Server 192.168.1.114 -AppPassword ...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Setup-Station.ps1" %*
pause
