# Enemy Roster

This folder contains the enemy prefabs used by room waves and sandbox scenes. Add an entry here only after an enemy has a working prefab with local settings, attack behavior, and spawn reference.

## Card Soldier

**Status:** Complete

**Prefab:** `Card_Soldier.prefab`

**Configuration:** Stored on the prefab Enemy component. Attack-specific settings are on CardChargeAttack.

**Attack:** `Assets/_Game/Scripts/Enemies/CardChargeAttack.cs`

### Role

A close-range pressure enemy that teaches the player to keep moving and sidestep committed charges.

### Visuals

- Plain white rectangular card.
- A random rank and suit are shown in the centre with a regular `TextMesh`.
- The card folds in half and uses its colour to show the pending attack: orange for a bite and red for a dash.
- During the dash, the folded card pulses as it moves along its locked path.
- A successful bite stops the dash and turns the card orange during recovery.
- It unfolds during recovery.

![Card Soldier concept art](../../Art/Characters/Enemies/CardSoldier/CardSoldierReference.png)

### Tunable Values

| Setting | Inspector location |
| --- | --- |
| Health and damage | Enemy / Core Stats |
| Follow speed | Enemy / Movement |
| Detection radius, alert duration, permanent awareness | Enemy / Detection |
| Roam radius, idle duration, patrol speed multiplier | Enemy / Patrol |
| Windup and recovery duration | Enemy / Attack Timing |
| Follow duration, charge speed and range, bite range, collision radius | CardChargeAttack / Charge Settings |
| Dash pulse amount and speed | CardChargeAttack / Visuals |
| Hit colour and duration | EnemyVisualFeedback |
| Health-bar height padding | EnemyUIAutoSetup |

Patrol speed is Follow Speed multiplied by Patrol Speed Multiplier; its calculated value is shown read-only. Card charges use Follow Duration instead of Enemy Attack Range. Permanent Awareness bypasses the chase leash. Enemy supplies Health and NavMeshAgent values when it spawns, so tune these through Enemy.

### Behaviour

1. Patrols around its spawn area until the player enters the detection radius or damages it.
2. Once alerted, it remains aware of the player and follows for the configured follow duration.
3. It stops, faces the player, and folds for the windup. It turns orange when the player is already in bite range and red when preparing to dash.
4. It locks its direction and dashes in a straight line with a pulsing movement effect.
5. The charge ends after a bite, collision with an obstacle, reaching a pit edge, or reaching its maximum travel distance.
6. A successful bite turns the Card Soldier orange and deals damage once.
7. It recovers for the configured recovery duration before following again.

### Spawn Integration

Card Soldier replaces the old Basic Enemy as the type-0 wave spawn in every room layout and in Dima's sandbox scene.

## Adding Future Enemies

For each completed enemy, add a section with its status, prefab, Inspector settings, attack script, role, visuals, tunable values, behavior loop, and spawn integration. Keep this roster focused on enemies that are ready for the game; work-in-progress ideas belong in design documentation instead.
