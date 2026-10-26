<#
  Setup-Server.ps1 - installs or upgrades the Pos database on the shop's SERVER PC (the PC that keeps the data).
  Run Setup-Server.cmd (double-click); it asks for administrator rights.

  Before: install PostgreSQL (EDB installer, Server + Command Line Tools) and the .NET 10 Desktop Runtime,
          and copy the program's publish folder to this PC (for example C:\PosInventory).

  New database (no "Pos" yet):
    1. creates it: the structure (Deploy\schema.sql), the latest functions (Database\*.sql) and the starting data
       (Deploy\seed.sql): the manager login (password asked here), walk-in customer, LBP + USD, register, company
  Existing database (an upgrade, after copying a newer program):
    1. runs Database\*.sql again: new columns and the latest functions; the data stays
  Both:
    2. id counters past the highest ids (Deploy\fix_sequences.sql)
    3. the program's own database user (pos_app, Deploy\app_user.sql): can use the data, cannot drop or change the database
    4. other PCs of the shop's network may connect, with the password (pg_hba.conf, listen_addresses, Windows Firewall
       for the local network only, the network set to Private)
    5. this PC's appsettings.json connects as pos_app
  At the end it shows what to give Setup-Station on the cashier PCs. Safe to run again.
#>
[CmdletBinding()]
param(
    [string]$Database = 'Pos',
    [int]$Port = 5432,          # PostgreSQL's port (another one only when two PostgreSQL versions are installed)
    [string]$AppUser = 'pos_app',
    [string]$CompanyName = '',
    [decimal]$UsdRate = 0,
    [string]$PostgresPassword = '',
    [string]$ManagerPassword = '',
    [string]$AppPassword = '',
    [switch]$SkipNetwork,       # this PC only: no other PC connects
    [switch]$SkipAppSettings    # leave appsettings.json as it is
)

$ErrorActionPreference = 'Stop'
$deploy = $PSScriptRoot
$root = Split-Path $deploy -Parent
$env:PGOPTIONS = '-c client_min_messages=warning'   # no "does not exist, skipping" notices
$env:PGCLIENTENCODING = 'UTF8'

function Write-Step([string]$text) { Write-Host ''; Write-Host "== $text" -ForegroundColor Cyan }
function Write-Ok([string]$text) { Write-Host "   $text" -ForegroundColor Green }
function Write-Warn([string]$text) { Write-Host "   $text" -ForegroundColor Yellow }

function Test-Admin {
    $principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

# The network part needs administrator rights: start again elevated, with the same options
if (-not $SkipNetwork -and -not (Test-Admin)) {
    Write-Host 'Administrator rights are needed (firewall and PostgreSQL settings). Windows will ask for them...'
    $passOn = @()
    foreach ($key in $PSBoundParameters.Keys) {
        $value = $PSBoundParameters[$key]
        if ($value -is [switch]) { if ($value.IsPresent) { $passOn += "-$key" } }
        else { $passOn += "-$key `"$value`"" }
    }
    Start-Process powershell.exe -Verb RunAs -ArgumentList ("-NoProfile -ExecutionPolicy Bypass -NoExit -File `"$PSCommandPath`" " + ($passOn -join ' '))
    exit
}

function Read-Secret([string]$prompt) {
    $secure = Read-Host -Prompt $prompt -AsSecureString
    $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try { return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr) }
}

# The program's password hash (PasswordHasher.cs): PBKDF2-SHA256, fixed app salt, 100 000 rounds, "pin$" + Base64
function Get-PasswordHash([string]$password) {
    $salt = [Text.Encoding]::UTF8.GetBytes('Inventory.POS.users.v1')
    $kdf = New-Object Security.Cryptography.Rfc2898DeriveBytes($password, $salt, 100000, [Security.Cryptography.HashAlgorithmName]::SHA256)
    try { return 'pin$' + [Convert]::ToBase64String($kdf.GetBytes(32)) } finally { $kdf.Dispose() }
}

# A password with letters and digits only: nothing to escape in the connection string
function New-AppPassword {
    $chars = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789'.ToCharArray()
    $bytes = New-Object byte[] 24
    [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    return -join ($bytes | ForEach-Object { $chars[$_ % $chars.Length] })
}

# --- PostgreSQL --------------------------------------------------------------------------------------------------
Write-Step 'PostgreSQL'
$pgBin = Get-ChildItem 'C:\Program Files\PostgreSQL\*\bin\psql.exe' -ErrorAction SilentlyContinue |
    Sort-Object { [int]($_.Directory.Parent.Name -replace '\D', '') } -Descending | Select-Object -First 1
if (-not $pgBin) { throw 'PostgreSQL was not found in C:\Program Files\PostgreSQL. Install it first (EDB installer: Server + Command Line Tools).' }
$psql = $pgBin.FullName
Write-Ok "found $psql"

if (-not $PostgresPassword) { $PostgresPassword = Read-Secret 'Password of the "postgres" user (chosen when PostgreSQL was installed)' }
$env:PGPASSWORD = $PostgresPassword

function Invoke-Sql([string]$db, [string[]]$arguments) {
    # Errors still show (they go to stderr); the rows some scripts print do not
    & $psql -h localhost -p $Port -U postgres -d $db -q -X -v ON_ERROR_STOP=1 @arguments | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "psql failed ($db): $($arguments -join ' ')" }
}
function Get-SqlValue([string]$db, [string]$sql) {
    $value = & $psql -h localhost -p $Port -U postgres -d $db -q -X -At -c $sql
    if ($LASTEXITCODE -ne 0) { throw "psql failed: $sql" }
    return ($value | Out-String).Trim()
}

try { $version = Get-SqlValue 'postgres' 'SHOW server_version;' }
catch { throw 'Could not log in to PostgreSQL as "postgres": check the password (and that the PostgreSQL service is running).' }
Write-Ok "PostgreSQL $version, logged in"

# --- Database ----------------------------------------------------------------------------------------------------
$exists = (Get-SqlValue 'postgres' "SELECT 1 FROM pg_database WHERE datname = '$Database';") -eq '1'
$scripts = Get-ChildItem (Join-Path $root 'Database\*.sql') | Sort-Object Name
if ($scripts.Count -eq 0) { throw "No Database\*.sql next to the program ($root\Database). Copy the whole publish folder." }

if (-not $exists) {
    Write-Step "New database `"$Database`""
    if (-not $ManagerPassword) {
        do {
            $ManagerPassword = Read-Secret 'Password of the first manager (the program''s login; 4 characters or more)'
            $again = Read-Secret 'The same password again'
            if ($ManagerPassword.Length -lt 4) { Write-Warn 'At least 4 characters.' }
            elseif ($ManagerPassword -ne $again) { Write-Warn 'The two passwords are not the same.' }
        } while ($ManagerPassword.Length -lt 4 -or $ManagerPassword -ne $again)
    }
    if ($UsdRate -le 0) {
        $typed = Read-Host 'Today''s dollar rate: 1 USD = how many LBP? (Enter = set it later in Settings > Currencies)'
        if ($typed) { $UsdRate = [decimal]::Parse(($typed -replace '[,\s]', ''), [Globalization.CultureInfo]::InvariantCulture) }
    }

    Invoke-Sql 'postgres' @('-c', "CREATE DATABASE `"$Database`" WITH ENCODING 'UTF8' TEMPLATE template0;")
    Write-Ok 'database created'
    Invoke-Sql $Database @('-f', (Join-Path $deploy 'schema.sql'))
    Write-Ok 'structure created (schema.sql)'
    foreach ($script in $scripts) { Invoke-Sql $Database @('-f', $script.FullName) }
    Write-Ok "functions: $($scripts.Count) scripts (Database\*.sql)"
    $hash = Get-PasswordHash $ManagerPassword
    Invoke-Sql $Database @('-v', "admin_hash=$hash", '-v', 'company=My Shop',
        '-v', ('usd_rate=' + $UsdRate.ToString([Globalization.CultureInfo]::InvariantCulture)), '-f', (Join-Path $deploy 'seed.sql'))
    Write-Ok 'starting data: manager "Administrator", walk-in customer, LBP + USD, register, company'
} else {
    Write-Step "Upgrading the existing database `"$Database`" (the data stays)"
    foreach ($script in $scripts) { Invoke-Sql $Database @('-f', $script.FullName) }
    Write-Ok "functions and new columns: $($scripts.Count) scripts (Database\*.sql)"
}

# The company name in a UTF-8 file, so an Arabic name arrives as typed
if ($CompanyName) {
    $file = Join-Path $env:TEMP 'pos-company.sql'
    [IO.File]::WriteAllText($file, "UPDATE public.company SET name = '" + $CompanyName.Replace("'", "''") + "' WHERE company_id = 1;", (New-Object Text.UTF8Encoding($false)))
    try { Invoke-Sql $Database @('-f', $file) } finally { Remove-Item $file -ErrorAction SilentlyContinue }
    Write-Ok "company name: $CompanyName"
}

Invoke-Sql $Database @('-f', (Join-Path $deploy 'fix_sequences.sql'))
Write-Ok 'id counters checked (fix_sequences.sql)'

# --- The program's database user ---------------------------------------------------------------------------------
Write-Step "Program database user `"$AppUser`""
$settingsFile = Join-Path $root 'appsettings.json'
if (-not $AppPassword -and (Test-Path $settingsFile)) {
    # Run again: keep the password the cashier PCs already use
    $current = [IO.File]::ReadAllText($settingsFile)
    if ($current -match "Username=$AppUser;Password=([A-Za-z0-9]+)") { $AppPassword = $Matches[1] }
}
if (-not $AppPassword) { $AppPassword = New-AppPassword }
Invoke-Sql $Database @('-v', "app_user=$AppUser", '-v', "app_password=$AppPassword", '-v', "db=$Database", '-f', (Join-Path $deploy 'app_user.sql'))
Write-Ok "$AppUser can use the data; it cannot drop or change the database"

# --- Network: the cashier PCs ------------------------------------------------------------------------------------
$serverIp = $null
if (-not $SkipNetwork) {
    Write-Step 'Network: let the shop''s other PCs connect'
    $config = Get-NetIPConfiguration | Where-Object { $_.IPv4DefaultGateway -and $_.NetAdapter.Status -eq 'Up' } | Select-Object -First 1
    if (-not $config) { throw 'This PC has no network connection with a router (gateway). Connect it to the shop network and run again.' }
    $address = $config.IPv4Address | Select-Object -First 1
    $serverIp = $address.IPAddress
    $prefix = [int]$address.PrefixLength
    $ipBytes = ([Net.IPAddress]::Parse($serverIp)).GetAddressBytes()
    [Array]::Reverse($ipBytes)
    $ipValue = [BitConverter]::ToUInt32($ipBytes, 0)
    $mask = if ($prefix -eq 0) { [uint32]0 } else { [uint32]([math]::Pow(2, 32) - [math]::Pow(2, 32 - $prefix)) }
    $netBytes = [BitConverter]::GetBytes([uint32]($ipValue -band $mask))
    [Array]::Reverse($netBytes)
    $cidr = "$(([Net.IPAddress]$netBytes).ToString())/$prefix"
    Write-Ok "this PC: $serverIp on $($config.InterfaceAlias), shop network $cidr"

    # PostgreSQL listens on the network
    $listen = Get-SqlValue 'postgres' 'SHOW listen_addresses;'
    $restart = $false
    if ($listen -ne '*') {
        Invoke-Sql 'postgres' @('-c', "ALTER SYSTEM SET listen_addresses = '*';")
        $restart = $true
        Write-Ok 'PostgreSQL will listen on the network (listen_addresses = *)'
    }

    # pg_hba.conf: the shop network, with a password
    $hba = Get-SqlValue 'postgres' 'SHOW hba_file;'
    if (Select-String -Path $hba -SimpleMatch $cidr -Quiet) {
        Write-Ok "pg_hba.conf already lets $cidr in"
    } else {
        Copy-Item $hba "$hba.bak-$(Get-Date -Format yyyyMMdd-HHmmss)"
        Add-Content -Path $hba -Encoding ascii -Value "`r`n# Pos: the shop's other PCs ($cidr), with a password`r`nhost    all             all             $cidr          scram-sha-256"
        Write-Ok "pg_hba.conf: $cidr may connect with a password (backup kept next to it)"
    }

    if ($restart) {
        $service = Get-Service -Name 'postgresql*' | Select-Object -First 1
        Restart-Service -InputObject $service -Force
        Start-Sleep -Seconds 3
        Write-Ok "PostgreSQL restarted ($($service.Name))"
    } else {
        Get-SqlValue 'postgres' 'SELECT pg_reload_conf();' | Out-Null
        Write-Ok 'PostgreSQL settings reloaded'
    }

    # A Public network blocks every incoming connection
    if ($config.NetProfile.NetworkCategory -eq 'Public') {
        Set-NetConnectionProfile -InterfaceIndex $config.InterfaceIndex -NetworkCategory Private
        Write-Ok "$($config.InterfaceAlias) is now a Private network"
    }

    # PostgreSQL's port from the local network only, never from the internet
    $ruleName = 'PostgreSQL (shop network)'
    if (Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue) {
        Write-Ok 'firewall rule already there'
    } else {
        New-NetFirewallRule -DisplayName $ruleName -Direction Inbound -Protocol TCP -LocalPort $Port `
            -RemoteAddress LocalSubnet -Profile Private, Domain -Action Allow | Out-Null
        Write-Ok "firewall: port $Port open to the local network only"
    }
}

# --- This PC's program settings ----------------------------------------------------------------------------------
if (-not $SkipAppSettings) {
    Write-Step 'This PC''s program settings'
    if (-not (Test-Path $settingsFile)) { throw "appsettings.json not found in $root" }
    $text = [IO.File]::ReadAllText($settingsFile)
    $connection = "Host=localhost;Port=$Port;Database=$Database;Username=$AppUser;Password=$AppPassword"
    $updated = [regex]::Replace($text, '"DefaultConnection"\s*:\s*"[^"]*"', ('"DefaultConnection": "' + $connection + '"'))
    if ($updated -eq $text -and $text -notmatch [regex]::Escape($connection)) { throw 'No "DefaultConnection" in appsettings.json: add it and run again.' }
    [IO.File]::WriteAllText($settingsFile, $updated, (New-Object Text.UTF8Encoding($false)))
    Write-Ok "appsettings.json connects to $Database as $AppUser"
}

# --- What the cashier PCs need ----------------------------------------------------------------------------------
Write-Host ''
Write-Host '==================================================================================' -ForegroundColor Green
Write-Host ' The server is ready.' -ForegroundColor Green
if ($serverIp) {
    Write-Host ''
    Write-Host ' On each cashier PC (after copying the program folder there), run:'
    $portArg = if ($Port -ne 5432) { " -Port $Port" } else { '' }
    Write-Host "   Deploy\Setup-Station.cmd -Server $serverIp$portArg -AppPassword $AppPassword" -ForegroundColor White
    Write-Host ''
    Write-Host " Keep this IP for this PC: reserve $serverIp for it in the router (DHCP reservation)."
}
Write-Host ''
Write-Host ' Open the program and log in with the manager password, then:'
Write-Host '   Settings > Company (name, logo), Currencies (dollar rate), Users, Receipt printer, Database backup.'
Write-Host '==================================================================================' -ForegroundColor Green
