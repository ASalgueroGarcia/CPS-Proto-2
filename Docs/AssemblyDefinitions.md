# Assembly definitions: what they'd buy us

v1 · 2026-10-01 · **proposal, not scheduled** · needs a team decision

## The problem

We keep working in each other's areas by accident, and nothing in the project
stops us. Three PRs were closed unmerged this month for exactly that reason. The
fix we've been using is a convention — "don't touch the player, Mario is on it" —
and conventions only hold while everyone remembers them.

Right now every script compiles into one blob (`Assembly-CSharp`). Shop code can
call the player, the player can call the UI, and nothing anywhere objects until a
human notices in review.

## What changes

An assembly definition per system (Player, Enemies, Waves, Map, Shop, UI, Core)
turns those boundaries into compile errors. After it:

- Calling into someone else's system fails to build unless you add a reference.
- Adding that reference is one line in an `.asmdef`, and it shows up in the diff
  where it can be discussed instead of discovered three weeks later.
- Unity only recompiles the assembly you touched, not all 61 scripts.
- PlayMode tests become possible at all — they need a test assembly, and a test
  assembly can't reference the one big blob we have today.

The boundary stops being a thing we remember and becomes a thing the compiler
enforces.

## What's in the way

Assemblies can't reference each other in a circle. `_Game/Scripts` had **seven**
such circles between its ten folders when this was written.

**This PR removes one of them**, to show what the work actually looks like.
`ShopManager` held a `UIManager` field that was assigned and never read, and
`PlayerStatsManager` held a `WaveManager` field whose only use was gating a
placeholder `Debug.Log`. Four lines of dead code were holding the Shop-to-UI and
Shop-to-Waves edges open. Deleting them closes the `Shop <-> UI` circle outright
and breaks the longer `Waves -> UI -> Shop -> Waves` chain. Six left.

**Three of the six come from two files in the wrong folder:** `Projectile.cs`
(enemy-facing, sitting in `Core/`) and `CombatDummy.cs` (a debug stub). PR #45
already deletes `CombatDummy`. Move `Projectile` to `Enemies/` and those three go
with it.

**That leaves three, all already on the cleanup list:**

| Circle | What causes it |
|---|---|
| Enemies <-> Player | `Enemy` searches the scene for `PlayerFSM` |
| Obstacles <-> Player | traps and breakables reach for `PlayerFSM` |
| Map <-> Waves | `SceneController` and `WaveManager` call each other directly |

Each is either *"something searches for the player"* or *"something calls another
manager directly"*. Both are on the cleanup list already, and the enemy one is
written — the dependency-injection change in the #40 chain replaces that search
with a reference handed over at spawn time.

So this isn't new work. It's the work we agreed to, plus a mechanism that stops
it coming back.

## Cost, and when

Three of the four circles are in the map, shop and player areas, so this is not
something one person can do alone — it needs whoever owns each area.

It also rewrites every file path, which makes any long-lived branch painful to
merge. That makes **now the wrong moment**: #45, #46 and #47 are open against
`dev`, and the #40 chain still hasn't reached `main`.

Suggested order:

1. Land #45 and #46, and get the #40 chain reconciled with `main`/`dev`
2. Mario's player refactor lands
3. Then assembly definitions, in a quiet window, split by area so each owner
   takes their own
4. PlayMode tests and CI on top

## The ask

Agreement in principle now, scheduled for the window after the player refactor —
not a decision to do it this sprint.
