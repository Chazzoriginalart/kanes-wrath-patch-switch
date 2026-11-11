$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot
Add-Type -AssemblyName System.Windows.Forms,System.Drawing,System.IO.Compression.FileSystem
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $repo 'build/Patch Switcher.exe'))
$type = $assembly.GetTypes() | Where-Object { $_.GetMethod('ImportMap',[Reflection.BindingFlags]'NonPublic,Static') }
$method = $type.GetMethod('ImportMap',[Reflection.BindingFlags]'NonPublic,Static')
$work = Join-Path $env:TEMP ('kwmaps-' + [guid]::NewGuid().ToString('N').Substring(0,8))
New-Item -ItemType Directory -Path "$work/input/Alpha","$work/input/Beta","$work/maps" -Force | Out-Null
Set-Content "$work/input/Alpha/Alpha.map" 'fixture alpha'
Set-Content "$work/input/Beta/Beta.map" 'fixture beta'
$result = $method.Invoke($null,@("$work/input","$work/maps"))
if (!(Test-Path "$work/maps/Alpha/Alpha.map") -or !(Test-Path "$work/maps/Beta/Beta.map")) { throw 'Nested folder import failed.' }
Write-Output "PASS: two nested map folders imported. $result"
