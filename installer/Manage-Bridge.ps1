[CmdletBinding(SupportsShouldProcess=$true)]
param(
    [Parameter(Mandatory=$true)][ValidateSet('Install','Uninstall')][string]$Mode,
    [Parameter(Mandatory=$true)][string]$GameDirectory
)
$ErrorActionPreference = 'Stop'
$game = (Resolve-Path -LiteralPath $GameDirectory).Path.TrimEnd('\','/')
$gameJar = Join-Path $game 'projectzomboid.jar'
if (!(Test-Path -LiteralPath $gameJar -PathType Leaf)) { throw 'Not a game directory: projectzomboid.jar missing.' }
$source = Join-Path $PSScriptRoot 'DropKickLoader.jar'
$target = Join-Path $game 'DropKickLoader.jar'
$receipt = Join-Path $game 'DropKickLoader.install.json'
$backupRoot = Join-Path $game 'DropKickLoader-backups'
foreach ($path in @($target,$receipt,$backupRoot)) {
    if ((Test-Path -LiteralPath $path) -and ((Get-Item -LiteralPath $path).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        throw "Refusing reparse point: $path"
    }
}
$old = $null
if (Test-Path -LiteralPath $receipt) {
    $old = Get-Content -Raw -LiteralPath $receipt | ConvertFrom-Json
    if ($old.product -ne 'DropKickPrivateBridge' -or $old.game -ne $game) { throw 'Unrecognized install receipt.' }
}
if (Test-Path -LiteralPath $target) {
    if (!$old -or (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $old.sha256) {
        throw 'Existing JAR is not owned by this installer or has changed. Nothing overwritten.'
    }
}
if ($Mode -eq 'Uninstall') {
    if (!$old) { throw 'No managed installation found.' }
    Write-Output 'Low-level bridge operation. Use Setup-Steam.ps1 for coordinated Steam setup/uninstall.'
    if ($PSCmdlet.ShouldProcess($target, 'Move bridge and receipt to recoverable backup (does not change Steam options)')) {
        $backup = Join-Path $backupRoot ([Guid]::NewGuid().ToString())
        $null = New-Item -ItemType Directory -Path $backup -Force
        if (Test-Path -LiteralPath $target) { Move-Item -LiteralPath $target -Destination (Join-Path $backup 'DropKickLoader.jar') }
        Move-Item -LiteralPath $receipt -Destination (Join-Path $backup 'DropKickLoader.install.json')
        Write-Output "Uninstalled; files recoverable in $backup"
    }
    return
}
if (!(Test-Path -LiteralPath $source)) { throw 'Packaged JAR missing.' }
# Inspect game files; never repair, delete, or replace other Java mods.
Add-Type -AssemblyName System.IO.Compression.FileSystem
$bundle = [IO.Compression.ZipFile]::OpenRead($source)
$vanilla = [IO.Compression.ZipFile]::OpenRead($gameJar)
try {
    $manifest = $bundle.GetEntry('compatibility.properties')
    if (!$manifest) { throw 'Bridge compatibility manifest missing.' }
    $reader = New-Object IO.StreamReader($manifest.Open())
    try { $lines = $reader.ReadToEnd() -split '\r?\n' } finally { $reader.Dispose() }
    foreach ($line in $lines) {
        if (!$line.Trim()) { continue }
        $pair = $line -split '=',2
        if ($pair.Count -ne 2 -or $pair[0] -notmatch '^[A-Za-z0-9_$/]+\.class$' -or $pair[1] -notmatch '^[a-f0-9]{64}(,[a-f0-9]{64})*$') { throw 'Invalid compatibility entry.' }
        $entry = $vanilla.GetEntry($pair[0])
        if (!$entry) { throw "Missing class: $($pair[0])" }
        $stream = $entry.Open(); $sha = [Security.Cryptography.SHA256]::Create()
        try { $actual = ([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-','').ToLowerInvariant() }
        finally { $stream.Dispose(); $sha.Dispose() }
        $allowed = $pair[1] -split ','
        if ($actual -ne $allowed[0]) { throw "Unsupported base game build: $($pair[0]). Nothing changed." }
        $override = Join-Path $game $pair[0]
        if (Test-Path -LiteralPath $override) {
            if ((Get-Item -LiteralPath $override).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Refusing linked override: $override" }
            if ((Get-FileHash -LiteralPath $override -Algorithm SHA256).Hash.ToLowerInvariant() -notin $allowed) {
                throw "Unrecognized override: $($pair[0]). Nothing changed."
            }
            Write-Output "Allowed compatible override: $($pair[0])"
        }
    }
} finally { $bundle.Dispose(); $vanilla.Dispose() }
$newHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
if ($PSCmdlet.ShouldProcess($target, 'Install/update private bridge; preserve prior managed version')) {
    $backup = Join-Path $backupRoot ([Guid]::NewGuid().ToString())
    $null = New-Item -ItemType Directory -Path $backup -Force
    if (Test-Path -LiteralPath $target) { Copy-Item -LiteralPath $target -Destination (Join-Path $backup 'DropKickLoader.jar') }
    if (Test-Path -LiteralPath $receipt) { Copy-Item -LiteralPath $receipt -Destination (Join-Path $backup 'DropKickLoader.install.json') }
    try {
        Copy-Item -LiteralPath $source -Destination $target -Force
        if ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $newHash) { throw 'Copy verification failed.' }
        $data = @{product='DropKickPrivateBridge'; game=$game; sha256=$newHash; installedUtc=[DateTime]::UtcNow.ToString('o')}
        [IO.File]::WriteAllText($receipt, ($data | ConvertTo-Json), (New-Object Text.UTF8Encoding($false)))
    } catch {
        # Do not delete anything on failure. Preserve evidence and the recovery path.
        throw "Install incomplete. Close game; restore any previous files from $backup. Original error: $_"
    }
    Write-Output "Installed and verified: $target"
}
Write-Output 'Bridge step complete. Setup-Steam.ps1 coordinates the Steam wrapper and launch options.'
