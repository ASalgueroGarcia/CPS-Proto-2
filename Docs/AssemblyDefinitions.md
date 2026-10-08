# Assembly definitions

v2 · 2026-10-08 · **implemented** · v1 (2026-10-01) was the proposal, agreed by the team

## What it is

Every feature folder under `Assets/_Game/Scripts` compiles into its own assembly,
`Spectracle.<Folder>`, defined by the `.asmdef` file in that folder. Its subfolders
(`Player/Weapons`, `Shop/POWERUPS`...) are part of the same assembly. A folder can
only use the folders its `.asmdef` lists. Using anything else is a compile
error, not something we hope review catches.

Unity also recompiles only the assembly you touched and the ones that use it,
not all of `_Game`.

## The graph

Read it top to bottom: a folder may only use folders **below** it, and only the
ones it lists. Nothing ever points up.

| Assembly | Uses | Packages |
|---|---|---|
| `Spectracle.Enemies` | UI, Player, Core | |
| `Spectracle.UI` | Map, Waves, Shop, Player, Core | Input System, TextMesh Pro, UGUI |
| `Spectracle.Map` | Waves, Core | UGUI |
| `Spectracle.Waves` | Pickups, Core | |
| `Spectracle.Pickups` | Shop, Core | |
| `Spectracle.Shop` | Player, Core | Input System, TextMesh Pro, UGUI |
| `Spectracle.Obstacles` | Player, Core | |
| `Spectracle.Player` | Core | Input System |
| `Spectracle.Core` | - | UGUI |
| `Spectracle.Environment` | - | |
| `Spectracle.Tests.Editor` | Waves, Core | NUnit, Test Runner, AI Navigation |

Every reference in that table is used: removing any one of them breaks the build.

`_Sandbox`, `ThirdParty` and the TextMesh Pro examples are not in any of these.
They stay in Unity's default `Assembly-CSharp`, which can use all of the above,
while nothing in `_Game` can use them. So deleting a sandbox can never break the
game, as RepoLayout already said, and now the compiler holds us to it.

## Day to day

**A new script** in an existing folder: nothing to do, it joins that folder's assembly.

**A new subfolder** inside a feature (`Player/Modules`): nothing to do, no `.asmdef`.

**A new feature folder** directly under `Scripts/`: copy an `.asmdef` from a sibling
folder, rename the file and the `name` inside it to `Spectracle.<Folder>`, and list
what it uses. Without one, its scripts fall into `Assembly-CSharp` and nothing in
`_Game` can see them.

**Editor tools** (custom inspectors, migration tools, anything with `using UnityEditor`)
go in `Scripts/Editor/`. It doesn't exist yet: whoever writes the first editor tool
creates it, **without** an `.asmdef`. Unity then compiles it into the editor-only
assembly, which can already see every `Spectracle.*` assembly. **Not** in a feature's own `Editor/` subfolder: inside a folder with an
`.asmdef`, `Editor/` is just a folder, so its code goes into the game assembly. The
editor still compiles, and then the build fails on `UnityEditor`.

**"The type or namespace X could not be found"** after you call another folder:
1. Check the graph. If the folder you are in sits *above* the one you need,
   add the reference: select your folder's `.asmdef`, add the other assembly under
   *Assembly Definition References*, Apply. Or add one line to the `references`
   list in the file. The line shows up in your PR, so it gets talked about.
2. If it sits *below* you, adding it makes a circle and Unity refuses. Turn the
   call around instead:
   - **The lower folder announces, the higher one listens.** `WaveManager` used to
     call `UIManager` to show the end-of-level canvas. It now raises
     `WaveManager.OnRoomCompleted`, and `UIManager` subscribes. A `static` event
     outlives the scene, so **subscribe in `OnEnable` and unsubscribe in `OnDisable`**.
     A destroyed listener left on the list still gets called. As soon as it touches its
     own GameObject it throws, and the listeners after it never run.
   - **An interface in Core.** The player's scissors used to look for
     `Breakable_Objects`, in Obstacles. They now look for `IBreakable` (Core),
     which `Breakable_Objects` implements.
   - **Whoever uses a number reads it; nobody pushes it.** The shop's
     "more enemies" / "more loot" items shouldn't call `WaveManager` (Waves is above
     Shop). Shop keeps the bonus in `PlayerStatsManager`, and `WaveManager` reads
     `PlayerStatsManager.Instance` when it spawns; Waves using Shop is allowed.
     Stunning enemies from a scissor hit goes through Core (`Health` or a Core
     interface), because Player can't see `Enemy`.

References are written by name (`"Spectracle.Core"`), not GUID, so a diff reads as
"Shop now uses Waves". Keep it that way: if the Inspector shows *Use GUIDs* ticked,
untick it before you add the reference.

**Tests:** `Assets/Tests/Editor` is `Spectracle.Tests.Editor`. To test a new area,
add its assembly to that `.asmdef`.

## What changed to get here

Assemblies can't use each other in a circle, and v1 counted seven circles. This
is everything that had to change for none to be left:

| Circle | Fix |
|---|---|
| Shop <-> UI, and Waves -> UI -> Shop -> Waves | Deleted a `UIManager` field in `ShopManager` and a `WaveManager` field in `PlayerStatsManager`, both dead (v1) |
| Core <-> Player, Core <-> UI | `CombatDummy` deleted in #45; `Projectile` moved from `Core/` to `Enemies/`, since only enemies fire it |
| Player <-> Obstacles | Scissors hit `IBreakable` instead of `Breakable_Objects` |
| Map -> Waves -> UI -> Map | `WaveManager.OnRoomCompleted` instead of calling `UIManager` |
| Enemies <-> Player | Already gone: the player no longer looks for `Enemy` |

Three `using Unity.VisualScripting;` lines that imported nothing were also deleted,
so no game assembly depends on Visual Scripting.

## Things to know

- **Scene and prefab diffs.** The next time someone saves a scene or prefab,
  lines like `m_EditorClassIdentifier: Assembly-CSharp::Enemy` become
  `Spectracle.Enemies::Enemy`, and button events' `Assembly-CSharp` becomes the
  new assembly name. That is Unity catching up, not a change. Commit it with the
  asset. Buttons keep working either way: Unity finds the method on the target
  object, not by the assembly name.
- **Enemies -> UI** is the one arrow pointing the wrong way: `Enemy` adds
  `EnemyUIAutoSetup` to itself (`Enemy.cs`, two lines). It isn't a circle today,
  but through UI it makes Enemies sit on top of almost everything, so **Waves,
  UI, Map, Shop and Pickups can't use `Enemy`**. A spawner that needs `Enemy`, a
  boss health bar or a stun item would hit that wall. The fix is planned for right
  after the player refactor (#53) lands, because #53 edits the same lines: put
  `EnemyUIAutoSetup` on the three enemy prefabs, delete the two lines, and drop
  UI from `Spectracle.Enemies.asmdef`. Enemies then sits just above Player.
- **Player is the bottom of the gameplay stack.** It uses only Core. Anything
  that reacts to the player (enemies, traps, pickups, shop, HUD) uses Player,
  never the other way round. That is what keeps the player safe to refactor.
