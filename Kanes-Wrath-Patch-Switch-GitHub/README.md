# Kane's Wrath Patch Switcher

Community tools for Command & Conquer 3: Kane's Wrath. Switch between R25i and the repaired R25j core, launch through Steam or the local game executable, and browse or import custom maps.

## R25j Fixes R3 Community Repack

[Download Setup-v2.exe from Google Drive](https://drive.google.com/open?id=1CBeP_MI1TMVld3dz2g4C1cWW1VGrC595)

[Project page on ModDB](https://www.moddb.com/mods/kanes-wrath-patch-switcher-r25j-fixes-r3-community-repack)

The single-file installer includes the repaired R25j gameplay core, four official R25j archives, and six custom maps. The 4K add-on is optional and separate. Close the game and WorldBuilder before installing or switching.

## Source layout

- `switcher/` contains the current Windows Forms app and PowerShell patch switching logic.
- `installer/` contains the earlier R3 installation engine and Setup Wizard source.

The switcher runs from the game folder and expects its script and local patch profiles under `R25j-repair`. Build with the .NET Framework C# compiler and reference System.Windows.Forms, System.Drawing, System.IO.Compression, and System.IO.Compression.FileSystem.

Thanks to Masterleaf and the Kane's Wrath mapmakers, testers, and players.
