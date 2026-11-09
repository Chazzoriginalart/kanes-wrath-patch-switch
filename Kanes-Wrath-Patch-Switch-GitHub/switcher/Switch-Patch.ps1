param(
    [switch]$Status,
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9 _.-]{0,63}$')]
    [string]$TargetPatch,
    [string]$ImportFolder
)

$ErrorActionPreference = 'Stop'
$patch = Join-Path $PSScriptRoot '../Patch103'
$profiles = @{
    'R25i' = Join-Path $PSScriptRoot 'R25i-Restore-2026-11-06/Payload/Patch103'
    'R25j Fixes R2' = Join-Path $PSScriptRoot 'Patch-Switcher-R25j-Fixes-R2'
}
$archives = @{
    'R25i' = $profiles['R25i']
    'R25j Fixes R2' = Join-Path $PSScriptRoot 'R25j-Full-Repaired/Payload/Patch103'
}
$registry = Join-Path $PSScriptRoot 'ExtraPatchProfiles'
foreach ($entry in Get-ChildItem -LiteralPath $registry -Filter '*.profile' -ErrorAction SilentlyContinue) {
    $profiles[$entry.BaseName] = (Get-Content -LiteralPath $entry.FullName -Raw).Trim()
    $archives[$entry.BaseName] = $profiles[$entry.BaseName]
}
# Apply the config last, after the shared archive filenames have been updated.
$files = '102Scripts.big', '102texturefix.big', 'config.txt'

function Get-Sha256($path) {
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        $stream = [IO.File]::OpenRead($path)
        try { [BitConverter]::ToString($sha.ComputeHash($stream)) }
        finally { $stream.Dispose() }
    } finally {
        $sha.Dispose()
    }
}

function Get-PatchHashes($directory) {
    foreach ($file in $files) {
        Get-Sha256 (Join-Path $directory $file)
    }
}

function Get-MapArchives($directory) {
    foreach ($line in Get-Content -LiteralPath (Join-Path $directory 'config.txt')) {
        if ($line -match '^add-big\s+(R25[ijm](?:1v1|2v2|3v34v4|Legacy)Maps\.big)\s*$') {
            $Matches[1]
        }
    }
}

function Get-ExtraArchives($directory) {
    foreach ($line in Get-Content -LiteralPath (Join-Path $directory 'config.txt')) {
        if ($line -match '^\s*$' -or $line -match '^add-search-path\s+MapData[\\/]\s*$') { continue }
        if ($line -notmatch '^add-big\s+([A-Za-z0-9][A-Za-z0-9 _.-]*\.big)\s*$') {
            throw "Unsupported config line: $line"
        }
        if ($Matches[1] -notin $files) { $Matches[1] }
    }
}

try {
    if ($ImportFolder) {
        $directory = (Get-Item -LiteralPath $ImportFolder).FullName
        $name = Split-Path -Leaf $directory
        if ($name -notmatch '^[A-Za-z0-9][A-Za-z0-9 _.-]{0,63}$' -or $profiles.ContainsKey($name)) {
            throw 'Use a unique folder name with letters, numbers, spaces, dots, underscores or hyphens.'
        }
        $extra = @(Get-ExtraArchives $directory)
        foreach ($file in $files + $extra) {
            if (-not (Test-Path -LiteralPath (Join-Path $directory $file) -PathType Leaf)) {
                throw "The selected folder is missing $file."
            }
        }
        if (-not $extra.Count) { throw 'The config must list at least one additional BIG archive.' }
        New-Item -ItemType Directory -Path $registry -Force | Out-Null
        New-Item -ItemType File -Path (Join-Path $registry "$name.profile") -Value $directory | Out-Null
        Write-Output "Added $name."
        return
    }
    if ($TargetPatch -and -not $profiles.ContainsKey($TargetPatch)) { throw 'Patch profile not found.' }
    $hashes = @{}
    foreach ($name in $profiles.Keys) {
        try { $hashes[$name] = (Get-PatchHashes $profiles[$name]) -join ',' } catch { if ($name -eq $TargetPatch) { throw } }
    }
    $live = (Get-PatchHashes $patch) -join ','
    $active = $profiles.Keys | Where-Object { $hashes[$_] -eq $live }
    if ($active) {
        Write-Output "Selected patch: $active"
    } else {
        $configured = @(Get-MapArchives $patch)
        $missing = @($configured | Where-Object { -not (Test-Path -LiteralPath (Join-Path $patch $_) -PathType Leaf) })
        if ($configured.Count -eq 4 -and @($configured -notmatch '^R25m').Count -eq 0) {
            Write-Output "Selected patch: R25m ($($missing.Count) of 4 map archives missing)"
        } else {
            throw 'The installed patch does not match a recognized configuration. No files were changed.'
        }
    }
    if ($Status) { return }
    $target = $TargetPatch
    if (-not $target) {
        Write-Output 'The first switch may take a few minutes to restore map archives.'
        Write-Output '  1. R25i'
        Write-Output '  2. R25j Fixes R2'
        Write-Output '  Q. Quit'
        $target = switch (Read-Host 'Choose a patch') {
            '1' { 'R25i' }
            '2' { 'R25j Fixes R2' }
            'Q' { return }
            default { throw 'Choose 1, 2, or Q.' }
        }
    }
    if (Get-Process -Name CNC3EP1, CNC3EP1.dat, WorldBuilder -ErrorAction SilentlyContinue) {
        throw "Close Kane's Wrath and World Builder before switching."
    }

    $lock = [IO.File]::Open((Join-Path $patch 'patch-switcher.lock'), 'OpenOrCreate', 'ReadWrite', 'None')
    try {
        if (((Get-PatchHashes $patch) -join ',') -ne $live) {
            throw 'The patch changed while the menu was open. Reopen the switcher.'
        }
        if ($target -in @('R25i', 'R25j Fixes R2')) {
            $required = @(Get-MapArchives $profiles[$target])
            if ($required.Count -ne 4 -or @($required | Select-Object -Unique).Count -ne 4) {
                throw "Expected four distinct map archives for $target."
            }
        } else {
            $required = @(Get-ExtraArchives $profiles[$target] | Select-Object -Unique)
            if (-not $required.Count) { throw 'The selected profile has no extra archives.' }
        }
        foreach ($archive in $required) {
            $source = Join-Path $archives[$target] $archive
            $destination = Join-Path $patch $archive
            if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
                throw "The saved map archive is missing: $archive"
            }
            Write-Output "Checking $archive..."
            $expected = Get-Sha256 $source
            if (Test-Path -LiteralPath $destination) {
                if ((Get-Sha256 $destination) -ne $expected) {
                    throw "The existing archive differs from the saved copy: $archive"
                }
                continue
            }

            Write-Output "Restoring $archive..."
            $staged = Join-Path $patch ([guid]::NewGuid().ToString() + '.partial')
            try {
                Copy-Item -LiteralPath $source -Destination $staged
                if ((Get-Sha256 $staged) -ne $expected) {
                    throw "The restored archive failed verification: $archive"
                }
                [IO.File]::Move($staged, $destination)
            } finally {
                if (Test-Path -LiteralPath $staged) { Remove-Item -LiteralPath $staged -Force }
            }
        }
        if ($active -eq $target) {
            Write-Output "$target is active and its map archives are ready."
            return
        }
        if (Get-Process -Name CNC3EP1, CNC3EP1.dat, WorldBuilder -ErrorAction SilentlyContinue) {
            throw "Close Kane's Wrath and World Builder before switching."
        }
        $work = Join-Path $patch ('switch-backup-' + [guid]::NewGuid())
        New-Item -ItemType Directory -Path $work | Out-Null
        $changed = @()
        try {
            foreach ($file in $files) {
                $staged = Join-Path $work "$file.new"
                $destination = Join-Path $patch $file
                Copy-Item -LiteralPath (Join-Path $profiles[$target] $file) -Destination $staged
                [IO.File]::Replace($staged, $destination, (Join-Path $work $file))
                $changed += $file
            }
            if (((Get-PatchHashes $patch) -join ',') -ne $hashes[$target]) {
                throw 'The installed files failed verification.'
            }
        } catch {
            foreach ($file in $changed) {
                Copy-Item -LiteralPath (Join-Path $work $file) -Destination (Join-Path $patch $file) -Force
            }
            throw
        } finally {
            Write-Output "Previous files saved in $work"
        }
        Write-Output "$target is now active."
    } finally {
        $lock.Dispose()
    }
} catch {
    Write-Output $_.Exception.Message
    exit 1
}




