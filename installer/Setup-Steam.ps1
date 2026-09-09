[CmdletBinding(SupportsShouldProcess=$true)]
param(
    [Parameter(Mandatory=$true)][ValidateSet('Install','Uninstall')][string]$Mode,
    [Parameter(Mandatory=$true)][string]$GameDirectory,
    [Parameter(Mandatory=$true)][string]$SteamLocalConfig
)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'SteamOptions.ps1')
if (Get-Process steam,ProjectZomboid64,java,javaw -ErrorAction SilentlyContinue) { throw 'Exit Steam and the game completely before setup. Do not merely close the Steam window.' }
$game = (Resolve-Path -LiteralPath $GameDirectory).Path.TrimEnd('\','/')
$config = (Resolve-Path -LiteralPath $SteamLocalConfig).Path
if ([IO.Path]::GetFileName($config) -ne 'localconfig.vdf') { throw 'Expected a Steam profile localconfig.vdf' }
$launcher = Join-Path $game 'DropKickSteamLaunch.exe'
$receipt = Join-Path $game 'DropKickSteamLaunch.install.json'
$source = Join-Path $PSScriptRoot 'DropKickSteamLaunch.exe'
foreach ($file in @($config,$launcher,$receipt)) {
    if ((Test-Path -LiteralPath $file) -and ((Get-Item -LiteralPath $file).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw "Refusing reparse point: $file" }
}
$text = [IO.File]::ReadAllText($config)
$slot = Get-DropKickLaunchSlot $text
$old = $null
if (Test-Path -LiteralPath $receipt) {
    $old = Get-Content -Raw -LiteralPath $receipt | ConvertFrom-Json
    if ($old.product -ne 'DropKickSteamLaunch' -or $old.config -ne $config) { throw 'Receipt/profile mismatch' }
    if (!(Test-Path -LiteralPath $launcher) -or (Get-FileHash -LiteralPath $launcher).Hash -ne $old.hash) { throw 'Managed launcher changed; refusing overwrite' }
    if ($slot.Raw -ne $old.installedRaw) { throw 'Steam options changed since setup. Review manually; nothing overwritten.' }
} elseif (Test-Path -LiteralPath $launcher) { throw 'Unmanaged launcher exists' }
if ($Mode -eq 'Uninstall' -and !$old) { throw 'No managed Steam entry found' }
$previous = Get-DropKickOptionValue $slot
if ($Mode -eq 'Install') {
    if (!(Test-Path -LiteralPath $source -PathType Leaf)) { throw 'Packaged launcher missing. Nothing changed.' }
    if (!$old -and ($previous.Contains('%command%') -or $previous -match '(?i)-javaagent:|-agentpath:|-agentlib:|(^|\s)--(\s|$)')) {
        throw 'Existing wrapper/JVM options need explicit review. Remove only obsolete DropKick options first; other options are not automatically changed.'
    }
    $baseOptions = if ($old) { $old.previousValue } else { $previous }
    $installedRaw = ConvertTo-DropKickVdf ('"' + $launcher + '" %command% ' + $baseOptions)
    $replacement = Set-DropKickOptionText $text $slot $installedRaw
} else {
    # Empty value is semantically identical when no original LaunchOptions key existed.
    $replacement = Set-DropKickOptionText $text $slot $old.previousRaw
}
Write-Output "Steam profile: $config"
Write-Output "Game directory: $game"
if (!$PSCmdlet.ShouldProcess($game,"$Mode DropKick bridge and Steam wrapper; backup original Steam config")) { return }
$backup = Join-Path $game ('DropKickSteamLaunch-backups/' + [Guid]::NewGuid())
$null = New-Item -ItemType Directory -Path $backup -Force
Copy-Item -LiteralPath $config -Destination (Join-Path $backup 'localconfig.vdf')
if ($Mode -eq 'Install') {
    & (Join-Path $PSScriptRoot 'Manage-Bridge.ps1') -Mode Install -GameDirectory $game
    if (Test-Path -LiteralPath $launcher) { Copy-Item -LiteralPath $launcher -Destination $backup }
    if (Test-Path -LiteralPath $receipt) { Copy-Item -LiteralPath $receipt -Destination $backup }
    Copy-Item -LiteralPath $source -Destination $launcher -Force
    $previousRaw = if ($old) { $old.previousRaw } elseif ($slot.Raw) { $slot.Raw } else { '""' }
    $record = @{product='DropKickSteamLaunch'; config=$config; hash=(Get-FileHash -LiteralPath $launcher).Hash; previousRaw=$previousRaw; previousValue=$baseOptions; installedRaw=$installedRaw}
    [IO.File]::WriteAllText($receipt,($record | ConvertTo-Json),(New-Object Text.UTF8Encoding($false)))
}
# Refuse a concurrent Steam write. Leave receipt and backup for recovery on any failure.
if ([IO.File]::ReadAllText($config) -cne $text) { throw "Steam config changed concurrently. Stop; recovery files: $backup" }
$temp = $config + '.dropkick-' + [Guid]::NewGuid() + '.tmp'
[IO.File]::WriteAllText($temp,$replacement,(New-Object Text.UTF8Encoding($false)))
# File.Replace requires its backup on the same volume as the destination.
# Steam and the game may be installed on different drives.
$configBackup = $config + '.dropkick-' + [Guid]::NewGuid() + '.bak'
[IO.File]::Replace($temp,$config,$configBackup)
Write-Output "Steam configuration recovery copy: $configBackup"
if ($Mode -eq 'Uninstall') {
    & (Join-Path $PSScriptRoot 'Manage-Bridge.ps1') -Mode Uninstall -GameDirectory $game
    Move-Item -LiteralPath $launcher -Destination $backup
    Move-Item -LiteralPath $receipt -Destination $backup
}
Write-Output "Done. Restart Steam. Use normal Play. Recoverable backup: $backup"
