using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class SceneController : MonoBehaviour
{
    public static SceneController Instance { get; private set; }

    [Header("Level Prefabs")]
    [SerializeField] private GameObject[] hl1Layouts;
    [SerializeField] private GameObject[] hl2Layouts;
    [SerializeField] private GameObject[] hl3Layouts;
    [SerializeField] private GameObject shopLevel;
    [SerializeField] private GameObject bossLevel;

    [Header("Map References")]
    [SerializeField] private Canvas mapCanvas;
    [SerializeField] private GameObject mapCamera;
    [SerializeField] private GameObject mapEventSystem;

    private GameObject spawnedInstance;
    private RoomType pendingRoom = RoomType.Medium;
    private Action<WaveManager> pendingSetup;
    private GameObject pendingLayout;

    private readonly Dictionary<GameObject[], List<GameObject>> bags = new Dictionary<GameObject[], List<GameObject>>();
    private readonly HashSet<GameObject> usedInNode = new HashSet<GameObject>();
    private GameObject[] earlyRows;
    private GameObject[] lateRows;
    private const string CurrentLevelScene = "SetScene";

    private void Awake()
    {
        Instance = this;
        earlyRows = hl1Layouts.Concat(hl2Layouts).ToArray();
        lateRows = hl2Layouts.Concat(hl3Layouts).ToArray();
        if (ScreenFader.Instance != null) ScreenFader.Instance.Set(0f);
    }

    public void LoadLevel(RoomType room, Action<WaveManager> setup = null, int mapRow = 0, bool firstOfNode = true)
    {
        SetMapVisible(false);
        pendingRoom = room;
        pendingSetup = setup;
        pendingLayout = PrefabFor(room, mapRow, firstOfNode);

        if (spawnedInstance) Destroy(spawnedInstance);
        if (SceneManager.GetSceneByName(CurrentLevelScene).isLoaded)
        {
            SceneManager.UnloadSceneAsync(CurrentLevelScene).completed += _ => LoadLevelScene();
            return;
        }

        LoadLevelScene();
    }

    public void UnloadLevel()
    {
        if (SceneManager.GetSceneByName(CurrentLevelScene).isLoaded)
        {
            SceneManager.UnloadSceneAsync(CurrentLevelScene);
        }
        SetMapVisible(true);
        Time.timeScale = 1f;
        if (spawnedInstance) Destroy(spawnedInstance);
    }

    private void LoadLevelScene()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(CurrentLevelScene, LoadSceneMode.Additive);
    }

    private void SetMapVisible(bool visible)
    {
        mapCanvas.gameObject.SetActive(visible);
        if (mapCamera != null) mapCamera.SetActive(visible);
        if (mapEventSystem != null) mapEventSystem.SetActive(visible);
        if (visible && ScreenFader.Instance != null) ScreenFader.Instance.Set(0f);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != CurrentLevelScene) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;

        spawnedInstance = Instantiate(pendingLayout);
        WaveManager waveManager = spawnedInstance.GetComponentInChildren<WaveManager>(true);
        if (waveManager != null)
        {
            waveManager.SetRoomType(pendingRoom);
            pendingSetup?.Invoke(waveManager);
        }

        // NOTE: Door transition cutscenes and room connections are placeholders while layouts are being aligned with Notion specifications.
        RoomDoorTransition door = FindFirstObjectByType<RoomDoorTransition>();
        if (door != null) door.Bind(waveManager);
    }

    private GameObject PrefabFor(RoomType room, int mapRow, bool firstOfNode)
    {
        switch (room)
        {
            case RoomType.Shop:
                return shopLevel;
            case RoomType.MiniBoss:
            case RoomType.Boss:
                if (bossLevel != null) return bossLevel;
                Debug.LogError("[SceneController] No boss level prefab assigned.", this);
                break;
        }

        if (firstOfNode) usedInNode.Clear();
        GameObject[] pool = firstOfNode ? hl1Layouts : mapRow <= 2 ? earlyRows : lateRows;
        GameObject layout = Draw(pool);
        usedInNode.Add(layout);
        return layout;
    }

    private GameObject Draw(GameObject[] pool)
    {
        if (!bags.TryGetValue(pool, out List<GameObject> bag))
        {
            bag = new List<GameObject>();
            bags[pool] = bag;
        }

        List<GameObject> candidates = bag.FindAll(l => !usedInNode.Contains(l));
        if (candidates.Count == 0)
        {
            bag.Clear();
            bag.AddRange(pool);
            candidates = bag.FindAll(l => !usedInNode.Contains(l));
        }

        if (candidates.Count == 0) candidates = bag;

        GameObject pick = candidates[Random.Range(0, candidates.Count)];
        bag.Remove(pick);
        return pick;
    }

    public Canvas GetMapCanvas()
    {
        return mapCanvas;
    }
}
