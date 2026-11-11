param([string]$OutputDirectory = (Join-Path $PSScriptRoot 'build'))
$ErrorActionPreference = 'Stop'
$csc = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if (!(Test-Path $csc)) { throw 'Install .NET Framework 4.x with its C# compiler.' }
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
foreach ($item in @(
    @{ Source='switcher/PatchSwitcher.cs'; Name='Patch Switcher.exe'; Target='winexe'; References=@('System.Windows.Forms.dll','System.Drawing.dll','System.IO.Compression.dll','System.IO.Compression.FileSystem.dll') },
    @{ Source='installer/ReleaseInstall.cs'; Name='ReleaseInstall.exe'; Target='exe'; References=@() },
    @{ Source='installer/SetupWizard.cs'; Name='Setup.exe'; Target='winexe'; References=@('System.Windows.Forms.dll','System.Drawing.dll') }
)) {
    $arguments = @('/nologo',('/target:' + $item.Target),('/out:' + (Join-Path $OutputDirectory $item.Name)))
    $arguments += @($item.References | ForEach-Object { '/reference:' + $_ })
    & $csc @arguments (Join-Path $PSScriptRoot $item.Source)
    if ($LASTEXITCODE -ne 0) { throw ('Compilation failed: ' + $item.Source) }
}
