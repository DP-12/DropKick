# Token-preserving VDF editor. Never serializes unrelated Steam settings.
function Get-DropKickLaunchSlot([string]$Text) {
    $tokens = [regex]::Matches($Text, '(?m)//[^\r\n]*|"(?:\\.|[^"\\])*"|[{}]') | Where-Object { !$_.Value.StartsWith('//') }
    $state = @{Index=0; Found=$null}
    function Read-Block([string[]]$Path) {
        while ($state.Index -lt $tokens.Count) {
            $keyToken = $tokens[$state.Index]; $state.Index++
            if ($keyToken.Value -eq '}') { return $keyToken.Index }
            if (!$keyToken.Value.StartsWith('"')) { throw 'Unsupported VDF structure' }
            $key = $keyToken.Value.Substring(1,$keyToken.Length-2)
            if ($state.Index -ge $tokens.Count) { throw 'Truncated VDF' }
            $value = $tokens[$state.Index]; $state.Index++
            if ($value.Value -eq '{') {
                $child = @($Path) + $key
                $close = Read-Block $child
                if ($child.Count -ge 2 -and $child[-2] -ieq 'apps' -and $key -eq '108600') {
                    if (!$state.Found) { $state.Found = @{Start=$close; Length=0; Raw=$null} }
                }
            } elseif ($Path.Count -ge 2 -and $Path[-2] -ieq 'apps' -and $Path[-1] -eq '108600' -and $key -ieq 'LaunchOptions') {
                if ($state.Found) { throw 'Duplicate launch options' }
                $state.Found = @{Start=$value.Index; Length=$value.Length; Raw=$value.Value}
            }
        }
        return $Text.Length
    }
    $null = Read-Block @()
    if (!$state.Found) { throw 'No Project Zomboid entry in this Steam profile. Launch it once normally first.' }
    return $state.Found
}
function ConvertTo-DropKickVdf([string]$Value) { return '"' + $Value.Replace('\','\\').Replace('"','\"') + '"' }
function Get-DropKickOptionValue($Slot) {
    if (!$Slot.Raw) { return '' }
    return [regex]::Replace($Slot.Raw.Substring(1,$Slot.Raw.Length-2), '\\([\\"])', '$1')
}
function Set-DropKickOptionText([string]$Text,$Slot,[string]$Raw) {
    if ($Slot.Length -eq 0) { $Raw = "`t`t`t`t`t`"LaunchOptions`"`t`t" + $Raw + "`r`n`t`t`t`t" }
    return $Text.Substring(0,$Slot.Start) + $Raw + $Text.Substring($Slot.Start+$Slot.Length)
}
