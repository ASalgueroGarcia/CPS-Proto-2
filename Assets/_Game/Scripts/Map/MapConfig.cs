using UnityEngine;

[CreateAssetMenu(fileName = "MapConfig", menuName = "Map/Map Config")]
public class MapConfig : ScriptableObject
{
    public MapGenerationSettings generation = new MapGenerationSettings();
    public MapRoomSettings rooms = new MapRoomSettings();
}