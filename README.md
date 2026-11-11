# Kane's Wrath Patch Switcher

Community tools for Command & Conquer 3: Kane's Wrath: switch patch profiles, launch the game, and browse/import maps.

## Downloads and source provenance

[Setup-v2 download](https://drive.google.com/open?id=1CBeP_MI1TMVld3dz2g4C1cWW1VGrC595) · [ModDB project](https://www.moddb.com/mods/kanes-wrath-patch-switcher-r25j-fixes-r3-community-repack)

The advertised Setup-v2 contains six custom maps. **The installer source here is the earlier R3 four-map engine (26 manifest entries), with source fixes. It does not establish what Setup-v2 contains or reproduce that binary.** The Setup-v2 source, payload, and build provenance must be supplied before claiming a matching reproducible release. Game patch archives and custom maps are not included in this source checkout.

## Build on Windows

Requires .NET Framework 4.x and its C# compiler. From PowerShell:

```powershell
./Build.ps1
./tests/Test-Regression.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ./tests/Test-Maps.ps1
```

Outputs are in `build/`: `Patch Switcher.exe`, `ReleaseInstall.exe`, and `Setup.exe`. Regression checks use disposable game folders and fixture-only process names; the shipped game/editor process guards remain enabled. They test unknown/missing core repair, invalid target rejection, backup cleanup, batch paths with spaces and trailing slashes, and SkuDef directive preservation. No multiplayer or graphical UI validation is claimed.

## Set up the switcher

Place `build/Patch Switcher.exe` beside your game's `CNC3EP1.exe`. Create `R25j-repair` beside it and copy `switcher/Switch-Patch.ps1` there.

Provide your own legally obtained, matching profile files in these folders under `R25j-repair`:

- `R25i-Restore-2026-11-06/Payload/Patch103`: R25i `102Scripts.big`, `102texturefix.big`, `config.txt`, and the four map BIG archives referenced by that config.
- `Patch-Switcher-R25j-Fixes-R2`: repaired R25j `102Scripts.big`, `102texturefix.big`, and `config.txt`.
- `R25j-Full-Repaired/Payload/Patch103`: the four R25j map BIG archives referenced by the repaired config.

Alternatively, use **Add patch folder** with a uniquely named folder containing both core BIG files, `config.txt`, and every additional BIG file that config references. Keep that folder available; the profile stores its absolute path. Config directives supported for added profiles are `add-big filename.big` and `add-search-path MapData/`.

Close the game and WorldBuilder before switching. Unknown or missing core files can be restored by explicitly selecting a valid target. Conflicting existing map archives are still rejected. Successful switches delete their temporary core backups; failed switches retain recovery files. Older backups are left for manual review.

## Build an earlier R3 installer package

Place `ReleaseInstall.exe`, `Setup.exe`, and `installer/Install.cmd` together with a `Payload` folder. Supply the original four-map R3 payload and `Payload/payload.sha256`, containing exactly 26 lines of `SHA256|relative/path`. Paths must begin with `Patch103/` or `CustomMaps/`. Do not substitute the six-map Setup-v2 payload. The source checkout alone is not an installable game patch.

## License

No project license has been declared by the owner. This change does not invent permissions for the code, game assets, or third-party maps. The owner should choose a code license and document asset permissions before redistribution.

Thanks to Masterleaf and the Kane's Wrath mapmakers, testers, and players.

