param(
    [Parameter(Mandatory=$true)][string]$JdkHome,
    [Parameter(Mandatory=$true)][string]$GameJar
)
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $JdkHome 'bin/javac.exe'
$archiver = Join-Path $JdkHome 'bin/jar.exe'
if (!(Test-Path -LiteralPath $compiler) -or !(Test-Path -LiteralPath $archiver)) {
    throw 'A JDK 25 with javac and jar is required; a JRE is not sufficient.'
}
$gameFile = (Resolve-Path -LiteralPath $GameJar).Path
$classes = Join-Path $PSScriptRoot 'build/classes'
$null = New-Item -ItemType Directory -Force -Path $classes
$sources = @(Get-ChildItem (Join-Path $PSScriptRoot 'src') -Recurse -Filter '*.java' | ForEach-Object FullName)
& $compiler --release 25 -encoding UTF-8 -cp $gameFile -d $classes @sources
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed; no new JAR was produced.' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($gameFile)
try {
    $fingerprints = foreach ($entryName in @(
        'zombie/Lua/LuaManager.class', 'zombie/Lua/LuaManager$GlobalObject.class',
        'zombie/Lua/LuaManager$Exposer.class',
        'se/krka/kahlua/integration/expose/LuaJavaClassExposer.class',
        'zombie/characters/IsoGameCharacter.class', 'zombie/characters/IsoZombie.class',
        'zombie/core/physics/Bullet.class', 'zombie/core/physics/RagdollController.class',
        'zombie/iso/objects/IsoZombieGiblets.class', 'zombie/iso/objects/IsoZombieGiblets$GibletType.class'
    )) {
        $entry = $archive.GetEntry($entryName)
        if (!$entry) { throw "Missing game class: $entryName" }
        $stream = $entry.Open()
        $sha = [Security.Cryptography.SHA256]::Create()
        try { $hex = ([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-','').ToLowerInvariant() }
        finally { $stream.Dispose(); $sha.Dispose() }
        if ($entryName -eq 'zombie/Lua/LuaManager$GlobalObject.class') {
            # Audited upstream ReflectionEnabler v2 for 42.20.2; excludes our old patched bridge.
            $hex += ',4915be279b76f53f0e2eaabe529ef026f23e8c0066a21f48f887a3cb99ad9f8f'
        }
        "$entryName=$hex"
    }
} finally { $archive.Dispose() }
[IO.File]::WriteAllLines((Join-Path $classes 'compatibility.properties'), $fingerprints)
& $archiver --create --file (Join-Path $PSScriptRoot 'build/DropKickLoader.jar') --manifest (Join-Path $PSScriptRoot 'MANIFEST.MF') -C $classes .
if ($LASTEXITCODE -ne 0) { throw 'JAR packaging failed.' }
Write-Output 'Experimental build only. Do not install into the live game before isolated verification.'
