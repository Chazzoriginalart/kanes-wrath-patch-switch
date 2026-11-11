# Source maintenance update

Repository contents now live at the root. Added Build.ps1 and fixture regression checks.
Fixed successful-switch temporary backup cleanup, explicit-target repair of unknown/missing core files, trailing-slash batch paths, nested map folder imports, and line-anchored SkuDef insertion.
Map imports run off the UI thread; import blocks dialog closure until completion. Thumbnail images are disposed and launch/map controls are themed. Removed unused rollback bookkeeping.

The earlier R3 installer retains its 26-file contract. Setup-v2 provenance and the project license remain unresolved; see README.md. UI appearance, multiplayer operation, and forced rollback failures have not been tested.

## Historical release notes
# R25j Fixes R3 Community Repack

The current Setup-v2 build contains the repaired R25j core, all four official R25j archives, and six custom maps.

- 2v2 — Blacksite Containment Chaos, Mosquito Coast Spillway, Tiberium Descent, Tiberium Scar Seepage, and Cat Tail Alley
- 4v4 — Twin Guardians Delta Force, with corrected blue veins at all four blue fields

Includes corrected Blacksite preview naming and the current Tiberium Descent artwork and terrain blends. The companion switcher supports R25i and the repaired R25j core, Steam launch, light and dark modes, and drag-and-drop map import.

[Download the installer](https://drive.google.com/open?id=1CBeP_MI1TMVld3dz2g4C1cWW1VGrC595)

[Project page on ModDB](https://www.moddb.com/mods/kanes-wrath-patch-switcher-r25j-fixes-r3-community-repack)

Installer SHA-256: 52ac33f4d8c58409ca44cefd5a574f64bb1ba92c6f6fa0f7d2bee728cf75143e

The package contents and matching patch files were verified. A full install and online match with this revision still need testing.

Thanks to Masterleaf and the Kane's Wrath community.

