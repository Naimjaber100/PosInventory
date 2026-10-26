<#
  Setup-Station.ps1 - connects a CASHIER PC (or another back-office PC) to the shop's server.
  Run Setup-Station.cmd; Setup-Server shows the exact line at its end, for example:
    Deploy\Setup-Station.cmd -Server 192.168.1.114 -AppPassword AbC123...
  Before: install the .NET 10 Desktop Runtime and copy the program's publish folder to this PC.
  It checks that the server can be reached, then writes the connection in this PC's appsettings.json.
  No administrator rights needed. Safe to run again (for example when the server's IP changed).
#>
[CmdletBinding()]
param(
    [string]$Server = '',
    [string]$AppPassword = '',
    [string]$Database = 'Pos',
    [int]$Port = 5432,
    [string]$AppUser = 'pos_app'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$settingsFile = Join-Path $root 'appsettings.json'

if (-not $Server) { $Server = Read-Host 'IP address of the server PC (shown at the end of Setup-Server, e.g. 192.168.1.114)' }
if (-not $AppPassword) { $AppPassword = Read-Host 'Database password (shown at the end of Setup-Server)' }
if ($AppPassword -notmatch '^[A-Za-z0-9]+$') { throw 'The password has letters and digits only: copy it again from the end of Setup-Server.' }

Write-Host ''
Write-Host "== Can this PC reach the server ($Server, port $Port)?" -ForegroundColor Cyan
$test = Test-NetConnection -ComputerName $Server -Port $Port -WarningAction SilentlyContinue
if ($test.TcpTestSucceeded) {
    Write-Host '   yes' -ForegroundColor Green
} else {
    Write-Host '   NO. Check that:' -ForegroundColor Yellow
    Write-Host '     - both PCs are on the same network (same Wi-Fi / router) and the server PC is on;'
    Write-Host "     - $Server is the server's IP (it shows it at the end of Setup-Server; it may have changed);"
    Write-Host '     - Setup-Server was run on the server WITHOUT -SkipNetwork (it opens the firewall).'
    $answer = Read-Host 'Write the settings anyway? (y/N)'
    if ($answer -notmatch '^[yY]') { exit 1 }
}

Write-Host ''
Write-Host '== This PC''s program settings' -ForegroundColor Cyan
if (-not (Test-Path $settingsFile)) { throw "appsettings.json not found in $root" }
$text = [IO.File]::ReadAllText($settingsFile)
$connection = "Host=$Server;Port=$Port;Database=$Database;Username=$AppUser;Password=$AppPassword"
$updated = [regex]::Replace($text, '"DefaultConnection"\s*:\s*"[^"]*"', ('"DefaultConnection": "' + $connection + '"'))
if ($updated -eq $text -and $text -notmatch [regex]::Escape($connection)) { throw 'No "DefaultConnection" in appsettings.json.' }
[IO.File]::WriteAllText($settingsFile, $updated, (New-Object Text.UTF8Encoding($false)))
Write-Host "   appsettings.json connects to $Database on $Server" -ForegroundColor Green
Write-Host ''
Write-Host 'Done. Open the program; then set this PC''s receipt printer in Settings > Receipt printer.' -ForegroundColor Green
