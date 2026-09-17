using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class SceneController : MonoBehaviour
{
    public static SceneController Instance { get; private set; }

    [Header("Level Prefabs")]
    [SerializeField] private GameObject[] combatLevels;
    [SerializeField] private GameObject shopLevel;
    [Tooltip("Layout used for the final boss node. Falls back to a random combat level if left empty.")]
    [SerializeField] private GameObject bossLevel;

    [Header("Map References")]
    [SerializeField] private Canvas mapCanvas;
    [SerializeField] private GameObject mapCamera;
    [SerializeField] private GameObject mapEventSystem;
    private GameObject _spawnedInstance;
    private NodeTypeEnum _pendingLevelType;
    private RoomType _pendingRoomTier = RoomType.Medium;
    private const string CurrentLevelScene = "SetScene";

    //public Action _onMapLoaded;

    private void Awake()
    {
        Instance = this;
    }

    
    public void LoadLevel(NodeTypeEnum levelType, RoomType roomTier)
    {
        //Debug.Log("LoadLevel called with: " + levelType);
        mapCanvas.gameObject.SetActive(false);
        if (mapCamera != null) mapCamera.SetActive(false);
        if (mapEventSystem != null) mapEventSystem.SetActive(false);
        _pendingLevelType = levelType;
        _pendingRoomTier = roomTier;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(CurrentLevelScene, LoadSceneMode.Additive);
    }

    public void UnloadLevel()
    {
        SceneManager.UnloadSceneAsync(CurrentLevelScene);
        //_onMapLoaded?.Invoke();
        mapCanvas.gameObject.SetActive(true);
        if (mapCamera != null) mapCamera.SetActive(true);
        if (mapEventSystem != null) mapEventSystem.SetActive(true);
        if (_spawnedInstance) Destroy(_spawnedInstance);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != CurrentLevelScene) return;
        //Debug.Log("OnSceneLoaded - pending type: " + _pendingLevelType);
    
        SceneManager.sceneLoaded -= OnSceneLoaded;
        GameObject prefab = null;

        switch (_pendingLevelType)
        {
            case NodeTypeEnum.Combat:
                //Debug.Log("Spawning combat level");
                prefab = combatLevels[Random.Range(0, combatLevels.Length)];
                break;
            case NodeTypeEnum.Merchant:
                //Debug.Log("Spawning shop level");
                prefab = shopLevel;
                break;
            case NodeTypeEnum.Boss:
                if (bossLevel != null)
                {
                    prefab = bossLevel;
                }
                else
                {
                    Debug.LogError("[SceneController] No boss level prefab assigned - falling back to a random combat layout. Assign 'Boss Level' on the SceneController.", this);
                    prefab = combatLevels[Random.Range(0, combatLevels.Length)];
                }
                break;
            default:
                //Debug.Log("Spawning default (combat) - type was: " + _pendingLevelType);
                prefab = combatLevels[Random.Range(0, combatLevels.Length)];
                break;
        }

        _spawnedInstance = Instantiate(prefab);

        // The tier has to reach WaveManager before its Start() runs InitializeRoom(),
        // which spawns the first wave immediately. Instantiate only runs Awake, so
        // writing it here - on the line after - is still ahead of Start.
        WaveManager waveManager = _spawnedInstance.GetComponentInChildren<WaveManager>(true);
        if (waveManager != null)
        {
            waveManager.SetRoomType(_pendingRoomTier);
        }
    }

    public Canvas GetMapCanvas()
    {
        return mapCanvas;
    }
}