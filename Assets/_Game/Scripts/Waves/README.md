# Scripts / Waves
WaveManager, RoomType, EnemyType - how rooms spawn and scale enemies.
Tune the numbers in Data/Rooms, not here: `RoomConfig_*` (tier: waves, legacy budget/rewards) and
`EncounterTable_Act1` (GDD In-Run Encounters + Rewards: cost, caps, First Spot caps, scene gating,
weights per scene, Threat Budget, Button/health/kill-drop rates).
`MapBehaviour.PlanRooms` passes each room's place in the run with `WaveManager.SetEncounterContext()`
(scene, node kind incl. Final Fight, First Spot, room in node) before Start(); without it the room uses
its tier's budget and rewards.
A spawner enemy (Deck of Cards) implements `IEnemySpawner` (Core) so its soldiers keep the room open.
Owner: Dima.
