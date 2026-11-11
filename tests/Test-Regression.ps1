$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot
$work = Join-Path $env:TEMP ('kwtest-' + [guid]::NewGuid().ToString('N').Substring(0,8))
$repair = Join-Path $work 'R25j-repair'
$patch = Join-Path $work 'Patch103'
$profile = Join-Path $work 'Fixture'
New-Item -ItemType Directory -Path $repair,$patch,$profile,(Join-Path $repair 'ExtraPatchProfiles') -Force | Out-Null
# Fixture-only process names allow tests while the real game/editor is open.
(Get-Content (Join-Path $repo 'switcher/Switch-Patch.ps1') -Raw).Replace('CNC3EP1, CNC3EP1.dat, WorldBuilder','KWFixtureNeverRunning') | Set-Content (Join-Path $repair 'Switch-Patch.ps1')
Set-Content (Join-Path $profile 'config.txt') 'add-big fixture.big'
Set-Content (Join-Path $profile '102Scripts.big') 'scripts'
Set-Content (Join-Path $profile '102texturefix.big') 'textures'
Set-Content (Join-Path $profile 'fixture.big') 'archive'
Set-Content (Join-Path $patch '102Scripts.big') 'unknown core'
Set-Content (Join-Path $repair 'ExtraPatchProfiles/Fixture.profile') $profile
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repair 'Switch-Patch.ps1') -TargetPatch Fixture
if ($LASTEXITCODE) { throw 'Unknown/missing-core switch failed.' }
foreach ($file in 'config.txt','102Scripts.big','102texturefix.big','fixture.big') {
    if ((Get-FileHash (Join-Path $patch $file)).Hash -ne (Get-FileHash (Join-Path $profile $file)).Hash) { throw "Mismatch: $file" }
}
if (@(Get-ChildItem $patch -Filter 'switch-backup-*').Count) { throw 'Successful switch retained temporary backup.' }
# Invalid target must leave live files unchanged.
Set-Content (Join-Path $profile 'config.txt') 'invalid config'
$before = (Get-FileHash (Join-Path $patch 'config.txt')).Hash
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repair 'Switch-Patch.ps1') -TargetPatch Fixture
if (!$LASTEXITCODE -or (Get-FileHash (Join-Path $patch 'config.txt')).Hash -ne $before) { throw 'Invalid target changed live config.' }
# Exercise the batch entry point with trailing backslashes and a try-add-config first line.
$game = Join-Path $work 'Game With Spaces'
$maps = Join-Path $work 'Maps'
$install = Join-Path $work 'installer'
$payload = Join-Path $install 'Payload'
New-Item -ItemType Directory -Path "$game/Core","$payload/Patch103",$maps -Force | Out-Null
Set-Content "$game/CNC3EP1.exe" 'fixture'
Set-Content "$game/CNC3EP1_english_1.2.SkuDef" "try-add-config Optional\config.txt`r`nadd-config Core\config.txt"
Copy-Item "$repo/installer/Install.cmd" $install
(Get-Content "$repo/installer/ReleaseInstall.cs" -Raw).Replace('"cnc3ep1","cnc3ep1.dat","worldbuilder"','"kwfixtureneverrunning"') | Set-Content "$install/ReleaseInstall.cs"
& "$env:WINDIR/Microsoft.NET/Framework64/v4.0.30319/csc.exe" /nologo ('/out:' + (Join-Path $install 'ReleaseInstall.exe')) (Join-Path $install 'ReleaseInstall.cs')
if ($LASTEXITCODE) { throw 'Fixture compilation failed.' }
$manifest = 1..26 | ForEach-Object {
    $file = "$payload/Patch103/fixture$_.big"
    Set-Content $file "fixture $_"
    (Get-FileHash $file).Hash + "|Patch103/fixture$_.big"
}
$manifest | Set-Content "$payload/payload.sha256"
$start = New-Object System.Diagnostics.ProcessStartInfo
$start.FileName = 'cmd.exe'
$start.UseShellExecute = $false
$start.Arguments = '/d /s /c ""' + (Join-Path $install 'Install.cmd') + '" "' + $game + '\" "' + $maps + '\""'
$process = [System.Diagnostics.Process]::Start($start)
$process.WaitForExit()
if ($process.ExitCode) { throw 'Batch installation failed.' }
$text = Get-Content "$game/CNC3EP1_english_1.2.SkuDef" -Raw
if ($text -notmatch '(?m)^try-add-config Optional' -or $text -notmatch '(?m)^add-config Patch103\\config.txt\r?$' -or $text -notmatch '(?m)^add-config Core') { throw 'SkuDef directives corrupted.' }
Write-Output "PASS: repair, target validation, backup cleanup, trailing paths, and SkuDef directives. Fixtures retained: $work"





