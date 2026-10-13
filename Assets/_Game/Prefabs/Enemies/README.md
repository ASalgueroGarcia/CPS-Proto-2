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

## White Rabbit

**Status:** Complete

**Prefab:** `White_Rabbit.prefab`

**Configuration:** Stored on the prefab Enemy component. Attack-specific settings are on WhiteRabbitAttack.

**Attack:** `Assets/_Game/Scripts/Enemies/WhiteRabbitAttack.cs` (spawns `ClockBomb.cs` at runtime)

### Role

A slow, tall kiter that keeps its distance and punishes standing still with a lobbed bomb or a thrown waist clock.

### Visuals

- Capsule body with two ears on top.
- Sphere at hand level representing the waist clock, connected by a stretched chain while thrown.
- Lobbed bomb spawns as a sphere with a flat circular ground AOE indicator that fills as the fuse ticks down.
- Body colour shifts orange (Clock Bomb) or red (Waist Clock) during the windup.

![White Rabbit concept art](../../Art/Characters/Enemies/WhiteRabbit/WhiteRabbitReference.png)

### Tunable Values

| Setting | Inspector location |
| --- | --- |
| Health and damage | Enemy / Core Stats |
| Follow speed | Enemy / Movement |
| Detection radius, alert duration, permanent awareness | Enemy / Detection |
| Roam radius, idle duration, patrol speed multiplier | Enemy / Patrol |
| Attack range, windup and recovery duration | Enemy / Attack Timing |
| Ideal distance, distance deadzone, kite duration | WhiteRabbitAttack / Kiting |
| Clock bomb chance | WhiteRabbitAttack / Attack Choice |
| Bomb damage, fuse time, radius, max throw range, flight time, arc height, knockback, scale, materials | WhiteRabbitAttack / Clock Bomb |
| Waist clock damage, throw speed, return speed, range, head hit radius | WhiteRabbitAttack / Waist Clock |
| Hit colour and duration | EnemyVisualFeedback |
| Health-bar height padding | EnemyUIAutoSetup |

Each attack uses its own damage value on WhiteRabbitAttack rather than Enemy / Core Stats damage.

### Behaviour

1. Patrols around its spawn area until the player enters the detection radius or damages it.
2. Backs away when the player is closer than Ideal Distance minus Deadzone and closes in when farther than Ideal plus Deadzone.
3. After the kite duration, once the player is inside Attack Range, it stops, faces the player, picks an attack, and shifts colour during the windup.
4. Clock Bomb: lobs a bomb to the player's position (clamped to max throw range) that lands on the ground, displays a filling AOE indicator circle, flashes faster over the fuse duration, and explodes.
5. Waist Clock: throws the clock head in a straight line and pulls it back along a visible chain after reaching max range or hitting an obstacle. Only the clock head deals damage, never the chain.
6. Recovers for the configured cooldown before kiting again.

### Spawn Integration

White Rabbit replaces the old Ranged Enemy (`Enemy_Ranged.prefab`) and preserves its prefab GUID (`50bcb89052d4caf47b00190ccc357fcf`).

Temporary: White Rabbit and Flower are both registered under the type-1 (Ranged) slot, cost 2, in `Layout_001` through `Layout_010`. `WaveManager` picks one of them at random (50/50) for each Ranged spawn, so both can be playtested. Revert to a single Ranged enemy once both are confirmed.

## Flower

**Status:** Complete

**Prefab:** `Flower.prefab`

**Configuration:** Stored on the prefab Enemy component. Attack-specific settings are on FlowerAttack.

**Attack:** `Assets/_Game/Scripts/Enemies/FlowerAttack.cs` (spawns `EnemyStraightProjectile.cs` at runtime)

### Role

A stationary ambusher that sinks underground, travels toward the player's last fixed position, and resurfaces committed to either a close-range bite or a ranged seed shot.

### Visuals

- Vertical green cylinder stem with an upright flat yellow circular head facing the player.
- Sinks into the ground during the burrow delay and hides while travelling underground.
- Dark ground strip marks the burrow path between the origin and current underground position.
- Head colour shifts orange (bite) or red (projectile) as it rises during the resurface windup.

![Flower concept art](../../Art/Characters/Enemies/Flower/FlowerReference.png)

### Tunable Values

| Setting | Inspector location |
| --- | --- |
| Health and damage | Enemy / Core Stats |
| Follow speed | Enemy / Movement |
| Detection radius, alert duration, permanent awareness | Enemy / Detection |
| Windup and recovery duration | Enemy / Attack Timing |
| Burrow delay, underground travel time, resurface offset, sink depth | FlowerAttack / Burrow |
| Bite range and bite damage | FlowerAttack / Bite |
| Projectile range, damage, speed, size, shoot point, material | FlowerAttack / Projectile |
| Attack duration | FlowerAttack / Timing |
| Visual root, crack material, width | FlowerAttack / Visuals |
| Hit colour and duration | EnemyVisualFeedback |
| Health-bar height padding | EnemyUIAutoSetup |

Follow Speed is kept at 0 so the Flower stays rooted while surfaced. Bite and projectile attacks use their own damage values on FlowerAttack.

### Behaviour

1. Stays rooted in place until the player enters the detection radius or damages it.
2. Faces the player and sinks into the ground over the burrow delay.
3. Locks the player's position at the moment it goes underground, becomes invulnerable, and travels underneath obstacles along a ground crack to an unobstructed point around that fixed position.
4. Resurfaces during the windup and checks distance to the player once to lock in its attack: orange for a bite when within Bite Range, or red for a straight seed shot when farther away.
5. Executes the locked-in bite or projectile shot without switching mid-attack.
6. Recovers during the configured cooldown before starting the next burrow cycle.

### Spawn Integration

Temporary: Flower shares the type-1 (Ranged) wave slot, cost 2, with White Rabbit across `Layout_001` through `Layout_010`. `WaveManager` picks between them at random (50/50) for each Ranged spawn. Revert to a single Ranged enemy once both are confirmed.

## Adding Future Enemies

For each completed enemy, add a section with its status, prefab, configuration, attack script, role, visuals, tunable values, behavior loop, and spawn integration. Keep this roster focused on enemies that are ready for the game; work-in-progress ideas belong in design documentation instead.
