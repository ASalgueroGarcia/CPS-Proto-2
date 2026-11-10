using UnityEngine;

/// <summary>Designer-tunable numbers for the scene map: how it is generated and which rooms its nodes play.</summary>
[CreateAssetMenu(fileName = "MapConfig", menuName = "Map/Map Config")]
public class MapConfig : ScriptableObject
{
    public MapGenerationSettings generation = new MapGenerationSettings();
    public MapRoomSettings rooms = new MapRoomSettings();
}