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

    [Header("Map References")]
    [SerializeField] private Canvas mapCanvas;

    private GameObject _spawnedInstance;
    private NodeTypeEnum _pendingLevelType;
    private const string CurrentLevelScene = "SetScene";

    //public Action _onMapLoaded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    
    public void LoadLevel(NodeTypeEnum levelType)
    {
        //Debug.Log("LoadLevel called with: " + levelType);
        mapCanvas.gameObject.SetActive(false);
        _pendingLevelType = levelType;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(CurrentLevelScene, LoadSceneMode.Additive);
    }

    public void UnloadLevel()
    {
        SceneManager.UnloadSceneAsync(CurrentLevelScene);
        //_onMapLoaded?.Invoke();
        mapCanvas.gameObject.SetActive(true);
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
            default:
                //Debug.Log("Spawning default (combat) - type was: " + _pendingLevelType);
                prefab = combatLevels[Random.Range(0, combatLevels.Length)];
                break;
        }

        _spawnedInstance = Instantiate(prefab);
    }
}