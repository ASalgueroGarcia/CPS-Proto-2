# Spectracle — Repo Layout Proposal

v2 · 2026-09-11 · draft for team review · comments → [issue #38](https://github.com/ASalgueroGarcia/CPS-Proto-2/issues/38)

v2 changes: `Art/Environment/` is now split into subfolders (Arena, Props, Obstacles,
Layouts, Textures), and §10 has a new `.gitignore` step (the repo ignores `*.md`).

## TL;DR

- Sort by **what it is**, not **who made it**
- All our content lives in `Assets/_Game/`
- Same feature names everywhere: `Prefabs/`, `Scripts/` and `Data/` use the same words
- Every folder has a `README.md` saying what goes in it and who owns it
- Personal experiments go in `Assets/_Sandbox/<Name>/`

**Why change:** per-person folders were fine for the prototype. Now features are
shared (for example, `Player.prefab` uses scripts from 3 people), so you can't find
anything unless you know who made it.

---

## 1. Repo root

```
CPS-Proto-2/
├─ README.md        ← start here
├─ Docs/
│  ├─ README.md     links to Notion
│  ├─ RepoLayout.md this doc
│  ├─ Design/       GDD
│  └─ Delivery/     uni hand-ins
├─ Assets/
├─ Packages/
└─ ProjectSettings/
```

Root `README.md`: what Spectracle is, the Unity version (6000.3.5f2), how to open
the project, the branch → PR → `main` rules, and a link to `Assets/_Game/README.md`.

---

## 2. Assets: top level

```
Assets/
├─ _Game/          all our content
├─ _Sandbox/       personal tests
├─ ThirdParty/     store packs
└─ TextMesh Pro/   Unity's, leave it
```

- `_` keeps our folders at the top of the Project window
- `_Sandbox/Ana/`, `/Antonio/`, `/Dima/`, `/Ivan/`: do anything in your own folder.
  **Nothing in `_Game` may use anything from `_Sandbox`**, so deleting a sandbox
  can never break the game.
- `ThirdParty/`: don't edit these files. Copy what you need into `_Game`.

---

## 3. Assets/_Game: full tree

```
_Game/
├─ README.md
├─ Scenes/        build scenes only
├─ Rooms/         room layouts
├─ Prefabs/
│  ├─ Player/
│  ├─ Enemies/
│  ├─ Pickups/
│  ├─ Obstacles/
│  ├─ Shop/
│  ├─ Map/
│  └─ UI/
├─ Data/          numbers to tune
│  ├─ Enemies/
│  └─ Items/
├─ Art/
│  ├─ Characters/
│  │  ├─ Player/
│  │  └─ Enemies/
│  ├─ Environment/
│  │  ├─ Arena/      stage, arena
│  │  ├─ Props/      wagons, decor
│  │  ├─ Obstacles/  breakables, traps
│  │  ├─ Layouts/    room plan images
│  │  └─ Textures/   floor, walls
│  ├─ Map/
│  ├─ UI/
│  │  └─ Icons/
│  └─ VFX/
├─ Audio/
│  ├─ Music/
│  ├─ SFX/
│  └─ Mixers/
├─ Scripts/       by feature
│  ├─ Core/
│  ├─ Player/
│  ├─ Enemies/
│  ├─ Waves/
│  ├─ Pickups/
│  ├─ Obstacles/
│  ├─ Shop/
│  ├─ Map/
│  ├─ UI/
│  ├─ Audio/
│  └─ Environment/
└─ Settings/      input, URP, volume
```

Rules that keep it simple:

- **Max 3 folders deep** below `_Game`
- **`Rooms/` is top level** because rooms are the main thing level design produces
- **`Data/`** holds the files designers edit to balance the game (enemy stats,
  item stats). Tune numbers there, not inside prefabs or scripts.
- **Art is grouped by thing**: everything for the player (model, textures,
  materials, animations) sits in `Art/Characters/Player/`, and everything for
  obstacles sits in `Art/Environment/Obstacles/`
- **`Core/`** is code shared by several features (Health, Projectile, CameraFollow)
- **No empty folders.** Create a folder when its first file arrives, with a README.

---

## 4. Where do I put…?

- a new room → `Rooms/`
- enemy stats → `Data/Enemies/`
- a new shop item → `Data/Items/`
- an obstacle model / material → `Art/Environment/Obstacles/`
- a prop (wagon, crate, decor) → `Art/Environment/Props/`
- any other model / texture / material → `Art/<what it is>/`
- a sound effect → `Audio/SFX/`
- a music track → `Audio/Music/`
- a UI icon → `Art/UI/Icons/`
- something you drop into a scene → `Prefabs/<feature>/`
- a script → `Scripts/<feature>/`
- a scene just for testing → `_Sandbox/<you>/`
- an Asset Store pack → `ThirdParty/<pack>/`
- not sure → ask in the group. Don't create a new top-level folder.

---

## 5. A README in every folder

Unity shows `.md` files in the Project window. Click one and the Inspector shows
its text, so the rules sit right next to the assets.

Template (max ~6 lines):

```
# Data / Enemies
One file per enemy type: all
its tunable numbers.
Not here: enemy prefabs → Prefabs/
Owner: Dima (ask before editing)
Naming: EnemyData_Heavy
New enemy: Ctrl+D an existing one,
  rename, change the numbers,
  assign it on the enemy prefab.
```

Every README answers the same 5 questions:
**What goes here · What doesn't · Owner · Naming · How to add one**

---

## 6. Naming: 3 rules

1. **No spaces or symbols** (`& - # ( )`):
   `Heavy Enemy.prefab` → `Enemy_Heavy.prefab`
2. **Category first, then details, joined by `_`:**
   `Room_Combat_001`, `SFX_Dash_01`, `Enemy_Ranged`
3. **English, spelled right.** The repo currently has
   `Enviroment`, `Brekeable` and `Scrissoring`.

Scripts keep normal C# naming: `PlayerFSM.cs`, one class per file, and the file
name matches the class name.

---

## 7. Owners (proposed, please confirm)

Ownership moves from "your folder" to the `Owner:` line in each README.
Optional: a GitHub `CODEOWNERS` file, so a PR that touches your folders asks you
for review automatically.

- **Dima**: Player, Enemies, Waves, Pickups, Core · Data/Enemies · Art/Characters
- **Antonio**: Map, Audio · Scenes · Art/Map
- **Ivan**: Shop, UI, Obstacles (code) · Data/Items · Art/UI, Art/VFX
- **Ana**: Rooms · Art/Environment · Obstacles (prefabs)
- **Everyone, ask first**: Settings, ThirdParty

---

## 8. What moves where

**Root folders**
- `FinalScenes/*` → `Scenes/`
- `Prefabs/*` → `Prefabs/<feature>/`
- `Animations/*` (scissors) → `Art/Characters/Player/`
- `Music/*` (they're scissor sounds) → `Audio/SFX/`
- `Extras/Settings/`, `InputSystem_Actions` → `Settings/`
- `VFX-Exp/` → `ThirdParty/`

**_DimaAssets**
- `_Scripts/*` → `Scripts/Player, Enemies, Waves, Pickups, Core`
- `EnemyData/` → `Data/Enemies/`
- `CharAnim/`, `Materials/` → `Art/Characters/`
- `Scenes/CharacterController` → `_Sandbox/Dima/`

**_AntonioAssets**
- `_Scripts/*` → `Scripts/Map/`, `Scripts/Audio/`
- `Prefabs/*Node*`, `Line` → `Prefabs/Map/`
- `Prefabs/Scenes/*` → `Rooms/`
- `Mats/` → `Art/Map/`
- `SFX/` → `Audio/SFX/` + `Audio/Mixers/`
- `Sprites/` → `Art/UI/`

**_IvanAssets**
- `_Scripts/SHOP/*`, `PlayerStatsManager` → `Scripts/Shop/`
- `_Scripts/OBSTACLES&TRAPS/` → `Scripts/Obstacles/`
- `_Scripts/UI/` → `Scripts/UI/`
- `Items/` → `Data/Items/`
- `-/` (icons) → `Art/UI/Icons/`
- `Scenes/SampleScene` → `_Sandbox/Ivan/`

**_AnaAssets**
- `RoomPrefabs/` → `Rooms/`
- `Assets/ObstaclePrefabs/` → `Prefabs/Obstacles/`
- `Assets/Arena 11.fbx`, `Enviroment_ActI_Final.fbx` → `Art/Environment/Arena/`
- `Assets/Theater Wagon*.fbx` → `Art/Environment/Props/`
- `Assets/Materials/` (Breakable, Unbreakable, Explosives, HurtingArea)
  → `Art/Environment/Obstacles/`
- `Assets/Materials/Floor.mat` → `Art/Environment/Textures/`
- `Assets/Images/Layout*` + their materials → `Art/Environment/Layouts/`
- other `Assets/Images/*` → `Art/Environment/Textures/`
- `Scripts/FollowingLights` → `Scripts/Environment/`
- `TestingScene` → `_Sandbox/Ana/`

**Docs**
- `game_design_doc.pdf` → `Docs/Design/`
- `Proyectos III - Entrega Preproducción 2.pdf` → `Docs/Delivery/`

---

## 9. Delete / clean up

- `_Recovery/`: Unity crash-backup scenes. Delete them and add the folder to `.gitignore`.
- `Extras/TutorialInfo/`, `Extras/Readme.asset`: leftovers from the URP template
- `Prefabs/NOT USED/`: git history keeps a copy anyway
- Old enemy scripts (`EnemyBase`, `BasicEnemy`, `HeavyEnemy`, `RangedEnemy`),
  once the enemy refactor is final
- NavMesh files: 3 copies across `FinalScenes/SetScene/` and
  `_AntonioAssets/Scenes/SetScene/`. Check which one SetScene uses; delete the rest.
- `TextMesh Pro/Examples & Extras/`: delete only if the UI doesn't use its fonts
- Stock images named `1000_F_…jpg`: rename them, and make sure they're licensed
  or clearly placeholders

---

## 10. How to migrate (≈1–2 h, one person)

1. The team agrees on this layout.
2. Everyone merges to `main` and stops working during the move (freeze).
3. One person moves the files **inside Unity** by dragging in the Project window.
   **Never** move them in Explorer or VS Code: each asset's `.meta` file has to move
   with it, or every reference to it breaks.
4. Commit in chunks: Scripts → Prefabs → Art → Audio → Data → Scenes.
   After each chunk, play MainMenu → Map → a room.
5. Update the hardcoded paths in `EnemyMigrationTool.cs`, or delete the tool if
   its job is done.
6. Fix `.gitignore`: it ignores `*.md` and `*.pdf` (lines 84–86), so READMEs
   wouldn't be committed. Add `!README.md` and `!Docs/**`. Move the PDFs with
   `git mv`, not Explorer, or git drops them.
7. Add the READMEs.
8. Open one PR → `main`. Then everyone starts a **fresh branch from the new
   `main`**. Don't keep working on an old branch.

Already checked in the code:
- Scenes load by name, so moving them is safe.
- None of our scripts use `Resources.Load`, so moving other assets is safe.
- When you move scenes inside Unity, the build scene list updates by itself.

---

## 11. Open questions

1. **Rooms:** Ana's `Layout_001–010` and Antonio's `CombatScene_001/002` both look
   like rooms. Which set is the real one?
2. **`_Sandbox`:** do we commit it (everyone can open your test scene) or
   git-ignore it (private)?
3. **`CODEOWNERS`:** do we want automatic review requests?
4. **Who does the move, and when is the freeze?**
