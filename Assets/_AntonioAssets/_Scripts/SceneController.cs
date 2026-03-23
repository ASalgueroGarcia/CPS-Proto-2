using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public static SceneController Instance { get; private set; }

    [Header("Level Prefabs")]
    [SerializeField] private GameObject[] combatLevels;
    [SerializeField] private GameObject shopLevel;

    private NodeTypeEnum _pendingLevelType;

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
        _pendingLevelType = levelType;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene("SetScene");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        var prefab = _pendingLevelType == NodeTypeEnum.Combat ? 
            combatLevels[Random.Range(0, combatLevels.Length)] : shopLevel;

        Instantiate(prefab);
    }
}