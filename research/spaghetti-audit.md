# Spaghetti & Frankenstein Audit — Spectracle (CPS-Proto-2)

**Date:** 2026-09-18 · **Audit branch:** `feature/cleanup` (off `feature/repo-restructure`)
**Scope:** `Assets/_Game/Scripts` (53 .cs files) + `_Game` scene/prefab/asset YAML + root docs
**Method:** 3 parallel read-only audits (duplication/Frankenstein · structure/coupling · dead code/inconsistencies). Every LIVE/DEAD verdict was verified by extracting each script's GUID from its `.cs.meta` and grepping it against all `.unity`/`.prefab`/`.asset` files under `Assets/_Game`. No runtime verification (Unity play mode not run).

---

## TL;DR

The folder restructure fixed *where things live*; the code inside still carries a year of uncoordinated work: ~800+ lines of verified-dead duplicate systems (an entire old enemy architecture coexisting with the new one), two god classes (`PlayerFSM` 664 lines, `UIManager` 465 lines) that everything reaches into, three competing "pause" implementations, and two refactor plans (`implementation_plan_fsm.md`, `implementation_plan_enem.md`) that were written but never executed. 9 cross-validated live bugs found along the way.

---

## Hotspot map (ranked)

| # | Hotspot | Severity | Key evidence |
|---|---------|----------|--------------|
| 1 | `Scripts/Player/PlayerFSM.cs` | Critical | 664 lines, ~12 responsibilities, static `IsPaused` global |
| 2 | `Scripts/UI/UIManager.cs` | Critical | 465 lines, UI owns game flow, triple-duplicated binding, duplicate singleton |
| 3 | Pause/time-scale anarchy | Critical | 3 representations, 6 write sites in 3 files |
| 4 | Unfinished enemy migration | High | ~563 dead duplicated lines + migration tool keeping them alive |
| 5 | Dual stats/health sources of truth | High | PlayerFSM ↔ PlayerStatsManager ↔ Health hand-synced |
| 6 | `WaveManager` + hardcoded `RoomConfigs` | High | balance table in static code, gameplay→UI singleton calls |
| 7 | AOE damage & knockback anarchy | High | 5 Overlap implementations, 3 knockback paths |
| 8 | Dead systems & orphaned content | Medium | GUID-verified zero references |
| 9 | Docs/hygiene residue | Medium | GEMINI.md 100% stale, phantom volume overrides, naming |

---

## 1. PlayerFSM.cs — the god class (664 lines)

**Responsibilities (≥12):** state machine (9–16, 203–220) · input handling (34–37, 148–164) · movement/gravity (225–245, 493–508) · dash + trail VFX + invulnerability (288–318, 606–619) · combo/attack combat & damage math (321–407) · runtime hitbox surgery (89–97, 102–120, 415–472) · animation control (27–32, 382–386, 621–628) · audio (74–77, 294, 359, 365, 372) · combo color feedback (351–405, 525–531) · AOE special attack (560–598) · knockback receiving (641–658) · public data surface polled by UI/stats (`speed`, `weaponBaseDamage`, `specialTimer`, `comboStep` — "Made public for UI access", line 123).

**Key smells:**
- `public static bool IsPaused` (130) — UI-owned concept living in the player class; written only by UIManager (391, 421, 435, 442), read only by PlayerFSM (267).
- `SetupScissorTrigger()` (102–120) adds Scissors + Rigidbody + Collider at runtime — exactly the debt `implementation_plan_fsm.md` ordered removed.
- `fallbackTimer` watchdog (66, 183–196, 347) — the plan's second deletion target, still present.
- `SwitchState()` (606–631) mixes transitions + invulnerability + `Physics.IgnoreLayerCollision` by layer-name string + animator.speed.
- `PerformNormalAttack()` — 321–407 (87 lines), ≥6 concerns, knockback 3/5/8 hardcoded (397–399), combo multipliers in code (362, 368–369).
- Duplicate hit-response entry points: `OnPlayerDamage` (172–177) vs `OnPlayerHit` (518–523) do the same thing.
- Debug residue: `[ContextMenu("Debug: Play Attack 3")]` (485–491), gizmo coroutine (576–583), 9 `Debug.Log` sites firing on every attack (385, 420, 442, 455–457, 461–462, 468, 562).
- Unused `public AudioSource audioSource` (23, auto-added 87) — all playback goes through SoundManager.

**Plan execution status (`implementation_plan_fsm.md`): 0% executed.** Remove `SetupScissorTrigger` — NOT DONE (102–120, invoked 90–92). Remove `fallbackTimer` — NOT DONE. Add `attackImpulseForce/Decay/Velocity` — NOT DONE (no such identifiers exist). Replace horizontal movement lock — NOT DONE (lock verbatim at 239–244). Keep animation-event handlers — DONE (the only no-op item). The file is the pre-plan version *plus* accretion the plan never anticipated.

## 2. UIManager.cs — UI that owns the game (465 lines)

**Responsibilities (≥10):** singleton + DontDestroyOnLoad (43–55) · pause/resume flow control incl. `Time.timeScale` (283–291, 411–436) · scene transitions / death-restart (387–404, 446–457, 459–464) · player stat reset orchestration (398–400) · scene-load bookkeeping (57–124) · HUD binding + per-frame refresh (126–142, 306–385) · reference discovery by object name (`FindChildByName` 246–257; TMP text by name substring 192–205) · HUD visibility by hardcoded scene names (144–162) · pause input (231–235) · writing `PlayerFSM.IsPaused` + `SceneController.Instance.UnloadLevel()` (391, 421, 435, 442–443).

**Key smells:**
- Player-binding block copy-pasted 3× in the same file: `OnSceneLoaded` (101–123), `Start` (171–188), `UpdatePlayerUI` (308–320).
- Duplicate singleton: `MainMenu.unity` has a direct UIManager component (line ~757) AND an instance of `UI.prefab` (which contains another UIManager); resolved at runtime by `SetActive(false); Destroy(gameObject)` (45–50) — order-dependent.
- Dead twin `ReturnToMap` (438–444): both `ReturnToMapBtn` objects in UI.prefab (component blocks ~1284 and ~1781) call `MapBtnBehaviour.ReturnToMap` via persistent onClick (lines 1396, 1893); no persistent call or code caller targets UIManager's copy. (`returnToMapBtn` field still used for visibility, 413.)
- Per-frame polling: `Update()` → `RefreshHUDVisibility()` every frame (296) + `UpdatePlayerUI()` rebuilds speed/damage/combo/coins/inventory strings every frame (306–385) despite `Health`'s event system existing.
- Hardcoded scene names: "SetScene" (83), "MainMenu"/"UI_Basic" (149 — UI_Basic does not exist anywhere in the project), "_MapScene" (403), "MainMenu" (413).

## 3. Pause/time-scale anarchy

"Paused" has three representations: `UIManager._isPaused`, `PlayerFSM.IsPaused` (static), `Time.timeScale`.

**`Time.timeScale` write sites (6, in 3 files):** `UIManager.cs:389, 419, 433, 440` · `MapBtnBehaviour.cs:23` · `ShopManager.cs:83, 90`.

**Divergence bug:** `ShopManager.OpenShop()` (83) sets `timeScale = 0` WITHOUT setting `PlayerFSM.IsPaused` — the player FSM's combat input checks (gated on `IsPaused`, `PlayerFSM.cs:267`) still run while the shop is open.

**Singleton idioms (4 different):** UIManager (static property + SetActive/Destroy guard, 43–55) · SoundManager (public static field, 21–35) · PlayerStatsManager (Instance property + guard, 42–51; persistence is accidental — stowaway on UIManager's DontDestroyOnLoad object, comment at 50) · SceneController (bare `Instance = this`, NO duplicate guard, 27–30).

**FindObjectOfType/Find-family calls:** MapBtnBehaviour.cs:13 · UIManager.cs:92, 115, 152, 166, 167, 180, 310, 321, 399 · CameraFollow.cs:25, 32 · Health.cs:169 · NodeBehaviour.cs:10 · Enemy.cs:104, 125 · EnemyBase.cs:65, 85 · PlayerStatsManager.cs:60, 108 · Node.cs:33 (`GameObject.Find("LineHolder")` — name-based global) · ShopInteractable.cs:19 · ShopManager.cs:38.

## 4. Unfinished enemy migration (~563 dead duplicated lines)

New system (LIVE, all 3 prefabs): `Enemy.cs` (294) + `IEnemyAttackStrategy` + `MeleeAttack`/`RangedAttack`/`DashAttack` + `EnemyAttackStateMachine` + `EnemyData` ScriptableObjects. Old system (DEAD, GUID-verified): `EnemyBase.cs` (224) + `BasicEnemy.cs` (129) + `HeavyEnemy.cs` (82) + `RangedEnemy.cs` (128).

**GUID verdicts:**
| Script | GUID | References | Verdict |
|---|---|---|---|
| EnemyBase.cs | 837365aac06ab214cba63a69d27da16e | none | DEAD |
| BasicEnemy.cs | e70d389cbb096664ca70b5ffa6ae8be6 | none | DEAD |
| HeavyEnemy.cs | 854222c12a4779e46b802b2926257c91 | none | DEAD |
| RangedEnemy.cs | efafb0ee8e7bbc940b61297453256a2a | none | DEAD |
| Enemy.cs | 3fd05e579df5eed469f1f4d2ed231401 | Enemy_Basic/Heavy/Ranged.prefab | LIVE |
| DashAttack.cs | a727ca6984888e34685521df4768d296 | Enemy_Basic.prefab | LIVE |
| MeleeAttack.cs | e34c75b2163b34e4abbd010d864d332d | Enemy_Heavy.prefab | LIVE |
| RangedAttack.cs | c4d796256068a854291d7b404a1f37fc | Enemy_Ranged.prefab | LIVE |
| EnemyData.cs | e5df469640ac03542816c93d52e54db3 | Data/Enemies/*.asset → prefabs | LIVE |

**Duplication:** ~200 of EnemyBase's 224 lines transcribed verbatim into Enemy.cs (enum, Awake, Start, Update, ExecuteBehaviorLoop, ChangeState, PatrolBehavior, GetNewPatrolTarget, MoveTowards/MoveAwayFrom, FlashColor/ResetColor, HandleDeath). Subclass↔strategy clones: BasicEnemy:28–45, 78–128 ↔ DashAttack:32–58, 70–108 (identical trail params); HeavyEnemy:73–81 ↔ MeleeAttack:35–45; RangedEnemy:92–127 ↔ RangedAttack:87–124 (incl. the SAME `PrimitiveType.Sphere` fallback at RangedEnemy.cs:108–116 / RangedAttack.cs:96–105 — which `implementation_plan_enem.md` §3 explicitly ordered removed from the new code).

**Blocker:** the four dead scripts are compile-time dependencies of `EnemyMigrationTool.cs` (52–54, 107–131, delete list 209–215). Its own menu item is "Delete Old Enemy Scripts (after verification)" (198) — never run. Migration itself IS complete (prefabs confirmed). Delete tool + scripts together (sanctioned by Docs/RepoLayout.md §9/§12).

**Enemy plan phase 2 (un-started):** no `SetTarget`, no `DistanceToPlayer`, no pooling anywhere; `IEnemyAttackStrategy.cs:15,27,34` still passes `float distanceToPlayer`.

## 5. Stats/health sources of truth

- `PlayerFSM` publics (speed, dashSpeed, weaponBaseDamage, baseCritChance — 41–51) mirrored by `PlayerStatsManager` privates (14–32), hand-synced in `BindToPlayer` (58–110), re-applied in `ApplyPowerUpEffect` 76-line switch (141–216, with dead cases 187–188, 200–210).
- Player health tracked twice: `Health` (authoritative) + `PlayerStatsManager.currentHealth/maxHealth` via `SyncHealth` (120–124); UIManager binds independently to `Health.OnHealthChanged` for the HUD.
- Listener-leak bug: `PlayerStatsManager.cs:63–69` — `healthPlayer` reassigned at 63 BEFORE `RemoveListener` at 68; old listener never removed.
- Dead stat fields: `currentCriticalDamage`/`currentSpecialDamage`/`currentAttackSpeed` (24–27) accumulated and printed but never applied.
- Spanish debug dump `Prints()` (231–241), call sites commented out (109, 215).

## 6. WaveManager.cs (336 lines, 3 classes in one file)

- Gameplay → UI singleton: `UIManager.Instance.ShowEoLCanvas()` at 243–244, 277–278.
- Spawns pickups (a Pickups-feature job): `SpawnRewards` 197–246.
- Whole room balance table hardcoded in static `RoomConfigs` (318–335) — contrast with the enemy team's `EnemyData` ScriptableObjects. `RoomConfig.baseWaveChance` (311) set in all 8 configs, never read.
- Wave-3 Slimo-only hack (90–93); `Invoke(nameof(SpawnWave), 2f)` magic delay (177).
- Ground-point logic duplicated within the file: `TryGetRandomPoint` (282–299) vs `TryGetValidPointNear` (248–270); also duplicated in dead `EnemySpawner.cs:31–63` with matching inspector fields and gizmos.
- `SpawnWave()` — 80–140 (61 lines), 4-level nesting.

## 7. AOE damage & knockback anarchy

**Five OverlapSphere/Box → damage implementations, each filtering victims differently:**
1. `PlayerFSM.ExecuteSpecialAttackDamage` (560–574, layer-mask, GetComponentInParent<Health>)
2. `Projectile.Explode` (85–111, tag-based, knockback via Health.TakeDamage)
3. `CombatDummy.HitPlayerBack` (52–67, DEAD)
4. `ExplodingTrap.ExplodeC` (56–79, tag-based, bypasses Health knockback via PlayerFSM.ApplyKnockback/rb.AddForce)
5. `FallingTrap.DamageZone` (57–70, DEAD)

**Three knockback paths:** `Health.ApplyKnockback` (105–132, Rigidbody-or-lerp) · `PlayerFSM.ApplyKnockback/KnockbackCoroutine` (641–658 — overwrites its `force` parameter with the `knockBackForce` field at 651) · ExplodingTrap manual AddForce.

**Two parallel HP systems:** `Health.TakeDamage(float,...)` vs `Breakable_Objects.TakeDamage(int)` (own hp counter, own death) — `Scissors.ProcessHit` special-cases both (35–64).

**Four hit-flash implementations:** Health.cs:90–103 (coroutine) · EnemyBase.cs:210–223 · Enemy.cs:261–274 (Invoke-based) · CombatDummy.cs:69–81 · plus PlayerFSM.SetPlayerColor (525–531, comment at 176 acknowledges overlap).

**Double-destroy seam:** `Health.Die()` (147–160) destroys AND both enemy systems' `HandleDeath` also destroys (EnemyBase 57–61, Enemy 276–280).

## 8. Dead code inventory (GUID-verified)

| Item | Evidence |
|---|---|
| `Scripts/Enemies/EnemyBase.cs` | zero refs; slated in RepoLayout §9 |
| `Scripts/Enemies/BasicEnemy.cs` | zero refs |
| `Scripts/Enemies/HeavyEnemy.cs` | zero refs |
| `Scripts/Enemies/RangedEnemy.cs` | zero refs; leftover commented code 122–123 |
| `Scripts/Enemies/Editor/EnemyMigrationTool.cs` | editor one-off; RepoLayout §12: "Delete the tool in the cleanup PR"; hardcoded paths 24–25, 209–215 |
| `Scripts/Pickups/EnemySpawner.cs` | only ref: _Sandbox/Dima/CharacterController.unity; duplicates WaveManager; also MISPLACED (Pickups folder) |
| `Scripts/Core/CombatDummy.cs` | only ref: Assets/Prefabs/NOT USED/Enemy-dummy.prefab |
| `Scripts/Obstacles/Pit.cs` | zero refs anywhere |
| `Scripts/Obstacles/FallingTrap.cs` | only ref: _Sandbox/Ivan/SampleScene.unity |
| `Assets/Prefabs/NOT USED/` | folder literally named NOT USED (RepoLayout §9) |
| `Prefabs/Map/CombatNode/MerchantNode/BossNode/MiniBossNode/TreasureNode.prefab` | unreferenced; _MapScene wires the *Btn variants; MiniBoss/Treasure pair with commented-out code (MapBehaviour.cs:31–32, NodeTypeEnum.cs:7–8) |
| `Rooms/CombatScene_001.prefab, CombatScene_002.prefab, ShopScene_001.prefab` | orphaned — SceneController wires only Layout_001–010 + shop_scene; contradicts RepoLayout §11 "real add-ons" claim; ShopScene_001 contains zero game scripts |
| Unreachable enum branches | `EnemyType.MiniBoss/MainBoss`, `RoomType.MiniBoss/Treasure` (no prefabs, commented node types) |
| `TrapBase`/`ITrap` | vestigial: every abstract method implemented as empty `{}` in all three traps (FloorSpikes 64–67, FallingTrap 71–74, ExplodingTrap 83–86); `TrapBase.TrapRadius` read by nothing — UNCERTAIN, team decision |

**Dead members in live scripts:** `Health.autoResetOnDeath` (20) never read · `Health.ResetAllHealthsInScene()` (167–171) never called · `RoomConfig.baseWaveChance` (311) · `Node._lbo` (28), `SetNodeIndex/GetNodeIndex` (173–181) · `MapBehaviour._currNodeIndex` (39, 317–320) · `SceneController.GetMapCanvas` (102) · `SoundManager.SetBgMusic` (42) · `Enemy.isMovingToPatrolPoint` (37) / `EnemyBase` (34) written-never-read · `FollowingLights.smoothTime/lockX/currentVelocity` (8–14) · `ShopInteractable.playerTag/playerInput` (9, 14) · `using Unity.VisualScripting;` in CameraFollow.cs:2, ShopInteractable.cs:1, ShopManager.cs:6 · `WorldSpaceHealthBar.Update()` literally empty (61–64) · `UIManager` commented slider logic (131–134) · commented blocks in FloorSpikes (10–11, 32–35, 47), MapBehaviour (31–32, 62, 193, 220), SceneController (25, 35, 48, 58, 85).

## 9. Coupling map & cycles

**Cross-feature edges (file:line creating them):** Player→Core (PlayerFSM→Health 22,136,568–569; →SoundManager 294,359,365,372; Scissors→Health 35,49) · Player→Obstacles (Scissors→Breakable_Objects 12,55–63) · Enemies→Player (Enemy→PlayerFSM 104; EnemyBase 65) · Enemies→Core (RangedAttack→Projectile 104,107; →SoundManager 114–115) · **Enemies→UI** (Enemy self-adds EnemyUIAutoSetup 52–53) · Waves→Core (→Health 150–153) · Waves→Pickups (→Coin 213–217, →HealthPickup 231–235) · **Waves→UI** (→UIManager.Instance 243–244, 277–278) · Map→Waves (SceneController→WaveManager 95–98; MapBehaviour/Node/SceneController→RoomType) · Map→UI (MapBehaviour wires Button.onClick 166, 248; MapBtnBehaviour writes timeScale 20–23) · UI→Player (UIManager reads/writes PlayerFSM 38, 92, 166, 310, 328–356, 391, 421, 435, 442) · UI→Shop (→PlayerStatsManager 37, 93, 107–110, 167, 321, 399–400) · UI→Map (→SceneController 443) · UI→Core (→Health 36, 104, 121, 176, 186) · Shop→UI (ShopManager→UIManager 30, 38, 102) · Shop→Player (PlayerStatsManager writes PlayerFSM fields 11, 60, 74–77, 90–93, 159–184) · Shop→Waves (dead placeholder 12, 108) · Shop→Core (→Health 63–136; →SoundManager 51) · Pickups→Shop (Coin 11, 15; HealthPickup 12, 14) · Pickups→Core (Coin→SoundManager 13) · Obstacles→Player (Breakable_Objects 23; ExplodingTrap 64–67) · Obstacles→Core (Health/SoundManager various) · Core→Player (CombatDummy 60 — dead; Projectile 73) · Core→Managers (Health→SoundManager 87, 157).

**Folder-level cycles:**
1. **UI ↔ Shop** (UIManager→PlayerStatsManager AND ShopManager→UIManager)
2. **UI → Map → Waves → UI** (UIManager→SceneController 443; SceneController→WaveManager 95–98; WaveManager→UIManager 243–244)
3. **Waves → Pickups → Shop → Waves** (last edge dead code but compiled)
4. **Player ↔ Obstacles** (Scissors→Breakable_Objects AND Breakable_Objects→PlayerFSM, plus ExplodingTrap→PlayerFSM)

## 10. Live bugs (each independently found by ≥2 auditors)

1. `UIManager.cs:340` — `cooldown.fillAmount += _playerFsm.specialTimer` accumulates every frame; `:348` sets `fillAmount = float.MaxValue`.
2. `Breakable_Objects.cs:23` — NRE whenever an ENEMY touches a breakable: tag check (21) lets "Enemy" through, then `GetComponent<PlayerFSM>().GetPlayerDamage()`.
3. `PlayerFSM.cs:651` — `KnockbackCoroutine` overwrites its `force` parameter with the `knockBackForce` field; `ExplodingTrap.cs:67`'s knockback argument silently discarded.
4. `Health.cs:157` — unguarded `SoundManager.Instance.PlaySound(deathClip)` (NRE if SoundManager missing; no deathClip null check) — the call at 87, three lines up, guards both.
5. `ExplodingTrap.cs:37–45` — yellow warning set then immediately overwritten by red; telegraph invisible.
6. `MapBehaviour.cs:210–217` — anti-duplicate reroll uses identical bounds, can re-pick the same node; `:273–274` — `nodeBtn.interactable = nodeBtn && ...` dereferences before null-check; `:190` — unbounded recursion if all layer-0 nodes are starting nodes.
7. `UIManager.cs:149` — checks scene name "UI_Basic" which exists nowhere in the project; `:363` — mojibake `\uFFFD` character in the coin HUD string.
8. `Rooms/shop_scene.prefab` — ShopManager `powerUpsA: []`: the live shop offers ZERO items despite 13 PowerUpData assets in Data/Items. Likely restructure wiring loss. UNCERTAIN — team decision.
9. `PlayerStatsManager.cs:63–69` — unsubscribe-after-reassign listener leak.

## 11. Docs, naming & hygiene

- **GEMINI.md: 100% of its 13 asset paths are stale** (pre-restructure `FinalScenes/`, `_IvanAssets/`, `_AntonioAssets/`, `_DimaAsstets/`, `Prefabs/--UI--.prefab`); also git-ignored (.gitignore:81) — local-only trap for AI CLIs. Correct paths now under `Assets/_Game/...`.
- **.gitignore:** CORRECTED (2026-09-21): this audit's original claim was wrong — the `!Docs/**` exception DOES exist (line 91, added in restructure commit `0232497`); Docs .md files are not ignored (verified via `git check-ignore`). The genuinely missing piece was only that 45 `README.md.meta` files were untracked (not ignored — simply never staged) — fixed in Phase 1 commit `99a2360`. 36 Unity folder `.meta` files remain untracked (restructure PR's business — flagged as follow-up).
- **Phantom volume overrides** in `Assets/_Game/Settings/DefaultVolumeProfile.asset`: `TestVolume`, `OasisFogVolumeComponent`, `TestAnimationCurveVolumeComponent`, `OutlineVolumeComponent` — script GUIDs resolve to nothing (Unity shows Missing (Mono Script)); plus a `VolumeComponentSupportedEverywhere` unit-test remnant. Likely VFX-pack experiment residue.
- **Misspelled tag "Breakeable"** (extra 'e'): `ProjectSettings/TagManager.asset:10`, `BreakableObject.prefab:20`, `Scissors.cs:55`. RepoLayout §6 claims misspellings were fixed — the class was, the tag wasn't.
- **Naming violations (RepoLayout §6):** class `Breakable_Objects` (underscore); one-class-per-file violations — `ScissorProxy` (Scissors.cs:101), `RoomConfig`+`RoomConfigs` (WaveManager.cs:308, 318), `NodeLayer` (MapBehaviour.cs:8); fields `Hitbox_attack12/3` (PlayerFSM 70–71), `Triggered`/`onCooldon` (FallingTrap 13–14), `EUI`/`aInteracted` (ShopInteractable 10, 12); lowercase enum members `enemyLoot/enemySpawn` (PowerUpData 40–41); `Scripts/Shop/POWERUPS/` all-caps residue; 10 `Data/Items/*.asset` with spaces ("Barbed Piruette", "Box Office", etc.) alongside correct `PowerUp_*.asset`; `_MapScene.unity` leading underscore; three naming styles in Rooms/ (`shop_scene` vs `ShopScene_001` vs `Layout_001`); `--POWERUPS TEXT--` object name.
- **Debug.Log spam:** PlayerStatsManager 13 (incl. Spanish `Prints()`), PlayerFSM 9 (fire on EVERY attack/hit), MapBehaviour 6, SceneController 6, Enemy 5 (every death), Node 3, WaveManager 3 (every spawn), SoundManager 3 (every SFX), Scissors 2 (every hit).
- **Root leftovers (pre-flagged cleanup-PR items, still present):** `Assets/Prefabs/NOT USED/`, `Assets/_Recovery/`, `Assets/Extras/` (URP template), 3 NavMesh copies in _Sandbox.
- **Pattern inconsistencies:** death wired via code-subscribed UnityEvent (enemies) vs inspector persistent call (player) vs C# events (pickups) — three styles for notifications; `public` fields vs `[SerializeField] private` mixed (both at once: `EnemySpawner.cs:19` `[SerializeField] public`); enemy layer as literal `6` (Scissors.cs:41) vs `LayerMask.NameToLayer("Enemy")` elsewhere; `ShopInteractable.cs:50–61` polls raw `Keyboard.current.eKey` bypassing the InputAction system; SoundManager null-guarded in some call sites, not others (checked: Health 87, RangedAttack 114, WaveManager 243/277; NOT checked: Health 157, Coin 13, FloorSpikes 41, Breakable_Objects 13, ButtonInteraction 10, PlayerFSM 294/359/365/372).
- **Runtime UI construction, three philosophies:** EnemyUIAutoSetup procedural canvas (66–139, unfinished comment 136–138) · UIManager name-based child hunting · ShopManager runtime CanvasGroup (40–43).
- `_Game` → `_Sandbox`/personal-folder references: **NONE** (verified by string grep + sandbox GUID check) — the §2 isolation rule holds.

## 12. Prioritized fix plan (branch: `feature/cleanup`)

**Phase 1 — Dead code purge (low risk, GUID-verified, sanctioned by RepoLayout §9/§12):** delete the 4 old enemy scripts + `EnemyMigrationTool` (tool is their only referencer — delete together), `EnemySpawner`, `CombatDummy`, `Pit`, `FallingTrap`, `Assets/Prefabs/NOT USED/`, 5 dead node prefabs, phantom volume overrides. Always delete `.meta` with the file. Fix `.gitignore` (`!Docs/**`, track README `.meta`s). Chunked commits; play MainMenu → Map → a room after each chunk.

**Phase 2 — Surgical bug fixes:** the 9 bugs in §10 (1–5 lines each, no refactoring). Bug #8 (empty shop) needs a team decision first.

**Phase 3 — Unify pause:** one `SetPaused(bool)` owner writing `timeScale` + `IsPaused` together; ShopManager and MapBtnBehaviour route through it.

**Phase 4 — Execute the existing plans:** `implementation_plan_fsm.md` (remove SetupScissorTrigger + fallbackTimer, add attack impulse) and `implementation_plan_enem.md` phase 2 (SetTarget injection, remove distanceToPlayer from interface, remove sphere fallback from RangedAttack.cs:96–105). Dedupe UIManager's triple binding into one method; remove the duplicate UIManager from MainMenu.unity.

**Phase 5 — Structural:** shared AOE-damage/knockback helper; `RoomConfigs` → ScriptableObjects in Data/; event-driven HUD; single source of truth for stats; rewrite GEMINI.md; naming sweep (BreakableObject class, POWERUPS folder, spaced item assets, Breakeable tag).

**Order rationale:** Phases 1–2 shrink the codebase ~800+ lines and fix user-visible breakage at near-zero risk; 3 removes a whole bug class; 4 is free design work (plans already written); 5 is the real de-spaghettification, done last on a clean base.

## 13. Confidence & open questions

**High confidence:** all DEAD/LIVE verdicts (GUID-grepped against every scene/prefab/asset under _Game); the 9 bugs (≥2 independent auditors each); pause multi-ownership; GEMINI.md staleness; FSM/enemy plans being unexecuted.

**Medium / needs team decision:**
- Shop `powerUpsA: []` — wiring loss or intentional placeholder?
- `CombatScene_001/002` + `ShopScene_001` — wire up or delete (RepoLayout §11 calls them "real add-ons" but nothing references them)?
- `TrapBase`/`ITrap` — vestigial, but deletion is a design call.
- `UIManager.ReturnToMap` — persistent-call wiring says dead, but serialized-call proof is not absolute.
- `EnemyUIAutoSetup` procedural fallback tier — may be unreachable depending on prefab wiring; needs Unity to verify.
- `PlayerStatsManager` placement — Scripts/Shop matches RepoLayout §8 as written, but "Core/Managers for cross-feature managers" (§3) suggests Core; doc is internally inconsistent.

**Not verified:** anything runtime (no Unity play mode). Phase 1's chunked playtests cover this.

---

## 14. Phase 1 execution log (2026-09-21, branch `feature/cleanup`, base `4da72cd`)

| Commit | What | Effect |
|---|---|---|
| `e2ce344` | Remove dead scripts (old enemy system, migration tool, CombatDummy, Pit) | 21 files, −911 lines; `FallingTrap` → `_Sandbox/Ivan/Scripts/`, `EnemySpawner` → `_Sandbox/Dima/Scripts/` (GUID-preserving moves — EnemySpawner references zero project types, so both sandbox scenes keep working) |
| `63da1b4` | Remove dead prefabs (orphaned map nodes and materials) | 5 node prefabs + 3 orphan materials, −1205 lines |
| `89cf5a4` | Remove NOT USED prefabs folder (dummy prefab) | −292 lines |
| `fd27ca8` | Strip phantom volume overrides from DefaultVolumeProfile | 9 blocks / −188 lines, pure deletions, 19/19 `m_Components` intact |
| `99a2360` | Track Unity .meta files for committed READMEs | 45 metas, +315 lines |

**Branch totals: 81 files, +298/−2562.**

**Corrections to this audit (found during execution):**
- `.gitignore`: the `!Docs/**` exception DOES exist (line 91, restructure commit `0232497`) — §11's original claim was wrong; only the `.md.meta` tracking was genuinely missing (fixed in `99a2360`).
- `DefaultVolumeProfile.asset` held 4 additional test-residue blocks beyond the 5 flagged (`CopyPasteTestComponent1/2/3`, `VolumeComponentSupportedOnAnySRP`) — removed in `fd27ca8`.

**Rulings made during execution:**
1. `FallingTrap`/`EnemySpawner` MOVED to their owners' sandboxes instead of deleted (keeps Ivan's/Dima's sandbox scenes alive; zero refs from `_Game`).
2. `Enemy-dummy.prefab` deleted despite 26 dangling instance refs in `_Recovery/0.unity` + Ivan's `SampleScene.unity` — the instances were already missing-script (CombatDummy gone); accepted cosmetic cost in a crash-backup and a personal sandbox scene.
3. The 4 extra test-residue volume blocks removed + amended into the same commit (same verified pattern: `m_Script {fileID: 0}` + test-assembly identifier).
4. `_Recovery/` and `Extras/` left untouched (RepoLayout §9 items, but outside the approved Phase 1 scope) — pending team decision.

**Final verification sweep (all pass):** zero references to `EnemyBase|BasicEnemy|HeavyEnemy|RangedEnemy|CombatDummy|EnemySpawner|\bPit\b|FallingTrap` in `Assets/_Game/Scripts`; CombatDummy script GUID zero refs in all of `Assets`; dummy-prefab GUID refs only in the two accepted scenes; working tree carries only pre-existing noise (`packages-lock.json` modified + 36 untracked folder metas).

---

## 15. Playtest findings (2026-09-21, post-Phase-1)

**Verdict: PASS** — full loop ran (MainMenu → Map → combat room → kill → rewards → Return to Map). No errors; no missing-script warnings in loaded scenes (the 26 dangling dummy refs only warn if `_Recovery/0.unity` or Ivan's SampleScene is opened manually).

**User-confirmed live bug:** #7 mojibake — TMP warning `\uFFFD not found in [InstrumentSerif-Regular SDF] … replaced by \u25A1 in [PlayerCoinsTMP]` → coin HUD renders `□` (`UIManager.cs:363`).

**New findings (runtime-only, missed by the static audit):**
- **Duplicate EventSystem** on Return to Map — "There can be only one active Event System" / "2 event systems", fired from `SceneController.UnloadLevel` (`SceneController.cs:51`). The additive room scene carries its own EventSystem alongside the persistent one.
- **AudioListener lifecycle wrong** — "2 audio listeners" during combat, "There are no audio listeners in the scene" after returning to Map → map/menu audio is dead after a room (the listener lived only in the unloaded scene's camera; the persistent camera has none).
- URP shadow-atlas resolution warnings (8 punctual lights > 2048² atlas) — cosmetic; optional Settings tweak.
- Debug.Log noise confirmed heavy in normal play (`SoundManager.cs:72` every SFX, `PlayerFSM.cs:385/442` every attack, `Node.cs:81` per node activation) — existing Phase 5 sweep item.

Backlog impact: mojibake already in Phase 2 (bug #7); EventSystem + AudioListener added as new Phase 2/5 candidates (both are small, but involve scene/prefab wiring — coordinate with the scene owner).

---

## 16. Phase 2 execution log (2026-09-22, branch `feature/cleanup`)

8 remaining audit bugs fixed (bug #8, the empty shop, was already fixed in `a9fed29`), committed in ownership chunks for CODEOWNERS review:

| Commit | Owner | Fixes |
|---|---|---|
| `1007183` | Dima (Core/Player) | `Health.Die` guards `SoundManager.Instance` + `deathClip` (mirrors the guarded call at line 87); `KnockbackCoroutine` honors its `force` parameter (ExplodingTrap's `knockbackEffect` was silently discarded); unused `knockBackForce` field removed |
| `14438f8` | Ivan (UI) | Cooldown fill = true recharge progress (`1 - specialTimer/specialCooldown`) instead of per-frame accumulation; READY sets fill to 1 (was `float.MaxValue`); dead `UI_Basic` scene check removed; U+FFFD coin char removed (playtest-confirmed box glyph) |
| `fbc873c` | Ivan (Shop/Obstacles) | `PlayerStatsManager` unsubscribes the OLD Health before reassigning (listener leak); `Breakable_Objects` null-guards the PlayerFSM lookup — enemy contact no longer NREs (enemy deals 1 damage; intent to be confirmed by Ivan); ExplodingTrap yellow telegraph now visible during the fuse, red flash at the blast |
| `f5c9908` | Antonio (Map) | Anti-duplicate child reroll now picks a different index from the neighbouring window when one exists (was identical bounds); `MakeStartingNodesInteractable` no longer dereferences a null Button; `ChooseStartingNode` does a bounded 10-attempt re-pick instead of unbounded recursion |

**Phase 2 totals: 7 files, +43/−33.** Full diff reviewed line-by-line before push.

**Deferred to Phase 5** (need scene/prefab surgery + scene-owner coordination): duplicate EventSystem on room unload (`SceneController.cs:51` area), AudioListener lifecycle (0 listeners on Map after unloading a room).

**Decisions made during Phase 2:** enemy-vs-breakable damage defaulted to 1 (the old code crashed there, so no prior behavior to preserve — Ivan to confirm); mojibake coin char replaced by plain number rather than guessing a glyph (font support unknown); UIManager.ReturnToMap dead-twin removal deferred to Phase 4/5 (touches inspector wiring on UI.prefab).

---

## 17. Dev playtest tool (2026-09-22, `f111abc`)

**DebugCheats (F1)** — `Scripts/Core/Debug/DebugCheats.cs`, born from the playtesting pain ("reaching the shop needs several rooms of play").

- Self-spawning via `RuntimeInitializeOnLoadMethod` → **zero scene/prefab wiring**, survives scene loads; guarded `#if UNITY_EDITOR || DEVELOPMENT_BUILD` → never ships.
- Buttons: Load Shop / Combat (all 8 `RoomType` tiers) / Boss · Return to Map · Kill All Enemies (waves cascade via real death events) · timescale 0.25/1/2 · +100 coins · Heal to Full · Open Shop UI instantly.
- Routes only through the game's own public APIs (`SceneController.LoadLevel/UnloadLevel`, `PlayerStatsManager.AddCoins/Heal`, `ShopManager.OpenShop`, `Health.TakeDamage`). When Phase 3 lands, timescale buttons should route through the unified pause API.
- Known v1 limits: cheat-return-to-map skips `MapBtnBehaviour.CompletedNode` (progression stays manual when jumping from the panel); if UIManager's pause was opened via ESC, a cheat timeScale reset can desync `UIManager._isPaused` — exactly the Phase 3 pause-ownership bug.
- README in the folder per layout rules; owner Dima. First in-Editor compile + playtest pending (Dima).
- **Playtest iterations (2026-09-22):** panel is now self-diagnosing — live status line (`room loaded / shop FOUND / active scene`) and a `[DebugCheats]` log on every press, because partial console pastes made diagnosis slow. Also: IMGUI clicks are invisible to the combat input system, so every panel press fired a player attack — the panel now sets `PlayerFSM.IsPaused` while open (third writer of that global; Phase 3 consolidates).

---

## 19. Phase 3 execution log (2026-09-22, `d6ffda0`)

**PauseManager** (`Scripts/Core/Managers/PauseManager.cs`) — the single owner. `SetPaused(bool)` writes `PauseManager.IsPaused` + `Time.timeScale` + the legacy `PlayerFSM.IsPaused` flag **atomically in one place**; `SetTimeScale(float)` exists for dev speed control. Static class, zero scene/prefab wiring.

**Routed:** UIManager (StartGame/Pause/Resume/ReturnToMap — the three identical unpause blocks collapsed to one call), `MapBtnBehaviour.ReturnToMap`, `ShopManager.OpenShop/HideShopLogic`, DebugCheats (room-load reset + timescale buttons). Kept raw **by design**: DebugCheats' F1 input-gate (`PlayerFSM.IsPaused = _visible`) — input blocking is not a pause (no timescale change); Phase 5 migrates readers and gives it a proper API. UIManager's own `_isPaused` menu-state field untouched (pause-MENU UI state, not game state).

**Verification:** grep sweep — all `Time.timeScale =` writes live in PauseManager only; all `PlayerFSM.IsPaused =` writes in PauseManager + the one documented DebugCheats gate. Compile + playtest pending (Dima).

**Shop decode pending (`3aeb3c5`):** static forensics found NO defect (all 17 prefab refs resolve in outer doc space; 13/13 assets valid; nested-prefab/loop-hang/two-instance hypotheses dead). Surviving: stale Library import (→ `[Shop] OpenShop: populating 0 item(s)`), runtime population exception (→ `population FAILED at slot N` + stack), manager shadowing (→ paste the two manager #IDs). Next playtest's `[Shop]` lines decide it in one run.

---

## 20. Phase 4 execution log (2026-09-23, `3a04742`..`a41ee9c`)

**Executed: `implementation_plan_enem.md` items 1–3 + `implementation_plan_fsm.md` phases 1–2.**

| Commit | Content |
|---|---|
| `3a04742` | Enemy DI: `SetTarget(Transform, Health)` + per-frame `DistanceToPlayer` property; WaveManager caches player once and injects on spawn (per-enemy `FindFirstObjectByType`/Tag search removed); `IEnemyAttackStrategy` no longer carries `distanceToPlayer` through 3 methods (strategies read `owner.DistanceToPlayer`); RangedAttack sphere-fallback → `LogError` + skip |
| `5317049` | RangedAttack.OnExecute signature matches cleaned interface |
| `01b3de7` | FSM plan 1.2: `fallbackTimer` failsafe removed (field + watchdog + 2 resets, −18 lines) |
| `698e11c` | FSM plan 1.1: `SetupScissorTrigger` removed (−23 lines); the kinematic Rigidbody it added at runtime is baked into Player.prefab (2 hitbox GameObjects already had BoxCollider(isTrigger)+Scissors — only the RB was runtime-added; 2 new Rigidbody docs, +36) |
| `a41ee9c` | FSM plan phase 2: Hades-style attack momentum — `attackImpulseForce=8/Decay=5/Velocity` fields, decaying surge in ApplyMovement replacing the absolute horizontal lock, finisher ×1.5 / special ×0.5 thrust, ResetCombo zeroes impulse |

**Deferred (enemy plan items 4–5):** animation-synced combat prep (needs an `EnemyAnimationForwarder` + SM "waiting for event" mode) and object pooling — new subsystems, own review cycles → Phase 5/6.

**Verification:** full strategy-interface consistency sweep (15 call sites clean); WaveManager line-endings normalized (CRLF); hitbox Rigidbody diff reviewed (pure additions); projectilePrefab confirmed wired on Enemy_Ranged (fallback removal is safe). Compile + playtest pending (Dima).

**Playtest focus:** attack feel (attacks now carry forward momentum — tune `attackImpulseForce/Decay` on the Player prefab if the lunge feels wrong), enemies engage normally (DI path), no `[WaveManager]`/`[Enemy]` errors, hitboxes hit as before (baked RB instead of runtime-added).

---

## 18. Task list addition: Debug.Log cleanup (Phase 5 sweep, scoped 2026-09-22)

Rule of thumb from Dima: strip spam, **keep anything that tells us what happened** (once-per-scene, per-purchase, per-wave, warnings).

**Kill (per-frame / per-attack / per-SFX spam):**
- `SoundManager.cs:72` — "PLAYING: … | pitch …" on **every** SFX (loudest offender)
- `PlayerFSM.cs:384` — "[COMBO] Playing Attack_…" every attack
- `PlayerFSM.cs:441` / `467` — "Activated Hitbox_attack12 specifically" / "Hitboxes DISABLED" every swing
- `PlayerFSM.cs:420, 455–462` — "[HITBOX DEBUG]…" hitbox lifecycle logs
- `PlayerFSM.cs:562` — "Executing Special Attack (Sphere AOE Only)"
- `Node.cs:81` — "Node X has N children" per node activation
- `WaveManager.cs:112` — per-enemy spawn line (replace with one per-wave summary if wanted)
- `Scissors.cs:52` — per-hit combat log

**Keep (they earned their place during playtesting):**
- `PlayerStatsManager` "Captured initial player stats." / "Applied persistent stats to new player." — once per scene; proved BindToPlayer runs
- `ShopManager` "Spent X coins" / "[Shop] Not enough coins for…" — the new economy feedback
- `PlayerFSM` "[FAILSAFE] stuck in …" warning — real stuck-state detector
- `SoundManager.PlayRandomSound` empty-list warning
- `Enemy.cs:278` "{name} killed." + `Health` "{name} has DIED!" — per-death, cheap, validated death flow (drop later if waves get a summary)
- `DebugCheats` logs — the tool's diagnostics

**Also in this pass:** `PowerUpSelect`'s silent `selectedPowerUp == null` early-return gets a log ("[Shop] Slot {l} is empty - open the shop first") — silent no-ops cost us a debugging session.
