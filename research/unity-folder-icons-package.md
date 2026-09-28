# Unity folder-icons packages — research

Date: 2026-09-17 · research by agent · context: CPS-Proto-2 restructure (Unity 6000.3.5f2)

## Decision

Install **com.simoxus.folder-icons** (Simoxus/folder-icons-for-unity), pinned to a commit.
`com.wooshii.foldericons` (used by the Overdose project) was rejected: dormant since 2021,
and the author himself filed an open bug "Unity 6 Support" (#11, Feb 2026).

## manifest.json line

```json
"com.simoxus.folder-icons": "https://github.com/Simoxus/folder-icons-for-unity.git#db0a63102116ec4982b206b204338e1f92fd1ab6"
```

- Package name: `com.simoxus.folder-icons` (hyphen, not `foldericons`)
- No releases/tags exist; pinned to latest commit `db0a631` (2026-07-21) for reproducibility
- No minimum Unity version declared; authored Jul 2026 against current-era Unity → Unity 6
  compatibility inferred (medium-high confidence), not officially declared
- Editor-only tooling (Preferences + projectWindowItemOnGUI) → small blast radius if it breaks

## Usage

- Enabled by default; **Alt+Click** a folder in the Project window → palette
  (color, ~90 bundled icons, built-in icons, custom icons; match by name or path, wildcards)
- Mappings auto-save to `Assets/Settings/Folder Icons/`
- Settings: Edit → Preferences → Folder Icons
- Pre-made mapping assets ship in the package's `Examples/` folder (~45, incl. Animations,
  Audio, Materials, Prefabs, Scenes) — copy desired ones into `Assets/Settings/Folder Icons/`

## Rejected alternative

`com.wooshii.foldericons`:

- Old URL `Wooshii/com.wooshii.foldericons` is dead (404); repo moved to
  `WooshiiDev/Unity-Folder-Icons`
- Latest release v0.1.4 (Oct 2021), all pre-releases, repo dormant
- Open issue #11 "Unity 6 Support" filed by the author (Feb 2026) — rendering risk on our editor
- Install requires manual "Import sample" + manually fixing texture alpha; known broken since 2021

Other candidates if ever needed: `Borod4r/Rainbow-Folders-2`, `Simoxus/folder-icons-for-unity`.
