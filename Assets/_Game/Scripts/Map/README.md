# Scripts / Map
The scene map and the rooms its nodes play.

Plain C#, tested in Tests/Editor:
- MapGenerator + MapGrid: roll the 8x4 map. `MapGenerator.Generate(rng, settings)`; same seed, same map.
- MapRoomPlanner: which rooms a node plays. `planner.Plan(type, row, sceneIndex, lastScene)` returns PlannedRoom entries (room type + RoomRewards).
- MapProgress: the player's route. `Enter(column)`, `StepBack()`, `StateOf(row, column)` returns a MapNodeState.

Scene side:
- MapBehaviour: draws the map, plays a node's rooms through SceneController, shows the HUD text.
- Node: one node button. SceneController: loads rooms. RoomDoorTransition + ScreenFader: walking in and out of a room (SetScene).

Numbers (paths, shop rows, type weights, room counts, rewards) live in Data/Map/MapConfig, not in these scripts.
Node prefabs/materials: Prefabs/Map, Art/Map.
Owner: Antonio, Miquel.
