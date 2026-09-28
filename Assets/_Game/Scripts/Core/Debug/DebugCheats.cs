// DEV-ONLY PLAYTEST CHEATS (F1) - stripped from release builds by the #if below.
// Self-spawns in every scene (RuntimeInitializeOnLoadMethod), so no scene/prefab wiring needed.
// Jump to any room, skip waves, add coins, toggle timescale. Routes through the same
// public APIs the game itself uses (SceneController, PlayerStatsManager, ...).
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class DebugCheats : MonoBehaviour
{
    private const string RoomSceneName = "SetScene";

    private bool _visible;
    private Vector2 _scroll;
    private RoomType[] _tiers;
    private string[] _tierNames;
    private int _tierIndex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (FindFirstObjectByType<DebugCheats>() != null) return;
        var go = new GameObject("~DEBUG CHEATS (F1)");
        DontDestroyOnLoad(go);
        go.AddComponent<DebugCheats>();
    }

    private void Awake()
    {
        _tiers = (RoomType[])Enum.GetValues(typeof(RoomType));
        _tierNames = Array.ConvertAll(_tiers, t => t.ToString());
        _tierIndex = Array.IndexOf(_tiers, RoomType.Medium);
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.f1Key.wasPressedThisFrame)
        {
            _visible = !_visible;
            // IMGUI clicks are invisible to the combat input system - every panel press
            // would also fire a player attack. Gating on IsPaused while the panel is open.
            PlayerFSM.IsPaused = _visible;
        }
    }

    private void OnGUI()
    {
        if (!_visible)
        {
            GUI.Label(new Rect(10, 10, 200, 20), "F1 - debug cheats");
            return;
        }

        bool roomLoaded = SceneManager.GetSceneByName(RoomSceneName).isLoaded;
        ShopManager shop = FindFirstObjectByType<ShopManager>();

        GUILayout.BeginArea(new Rect(10, 10, 250, 540));
        GUILayout.BeginVertical("box");
        GUILayout.Label("DEBUG CHEATS (dev build only)");
        _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(480));

        GUILayout.Label($"room loaded: {roomLoaded} | shop: {(shop != null ? "FOUND" : "none")}");
        GUILayout.Label($"timescale {Time.timeScale:0.00} | scene: {SceneManager.GetActiveScene().name}");

        // ---- ROOMS ----
        GUILayout.Space(6);
        GUILayout.Label("ROOMS");
        if (GUILayout.Button("Load Shop Room")) TryRun(() => LoadRoom(NodeTypeEnum.Merchant, RoomType.Shop));
        _tierIndex = GUILayout.SelectionGrid(_tierIndex, _tierNames, 2);
        if (GUILayout.Button("Load Combat Room (tier above)")) TryRun(() => LoadRoom(NodeTypeEnum.Combat, _tiers[_tierIndex]));
        if (GUILayout.Button("Load Boss Room")) TryRun(() => LoadRoom(NodeTypeEnum.Boss, RoomType.Boss));
        if (GUILayout.Button("Return to Map")) TryRun(ReturnToMap);

        // ---- TIME ----
        GUILayout.Space(6);
        GUILayout.Label("TIME");
        if (GUILayout.Button("Kill All Enemies")) TryRun(KillAllEnemies);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("x0.25")) TryRun(() => { PauseManager.SetTimeScale(0.25f); Log("timescale 0.25"); });
        if (GUILayout.Button("x1")) TryRun(() => { PauseManager.SetTimeScale(1f); Log("timescale 1"); });
        if (GUILayout.Button("x2")) TryRun(() => { PauseManager.SetTimeScale(2f); Log("timescale 2"); });
        GUILayout.EndHorizontal();

        // ---- PLAYER ----
        GUILayout.Space(6);
        GUILayout.Label("PLAYER");
        if (GUILayout.Button("+100 Coins")) TryRun(() =>
        {
            if (PlayerStatsManager.Instance != null) { PlayerStatsManager.Instance.AddCoins(100); Log("+100 coins"); }
            else Log("PlayerStatsManager not found!");
        });
        if (GUILayout.Button("Heal to Full")) TryRun(() => { PlayerStatsManager.Instance?.Heal(99999f); Log("heal to full"); });

        // ---- SHOP ----
        GUILayout.Space(6);
        GUILayout.Label("SHOP");
        if (GUILayout.Button("Open Shop UI")) TryRun(() =>
        {
            if (shop != null) { shop.OpenShop(); Log("OpenShop called"); }
            else Log("no ShopManager in this scene - Load Shop Room first");
        });
        GUI.enabled = shop != null;
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Buy Slot 0")) TryRun(() => { Log("Buy Slot 0"); shop.OpenShop(); shop.PowerUpSelect(0); });
        if (GUILayout.Button("Buy Slot 1")) TryRun(() => { Log("Buy Slot 1"); shop.OpenShop(); shop.PowerUpSelect(1); });
        if (GUILayout.Button("Buy Slot 2")) TryRun(() => { Log("Buy Slot 2"); shop.OpenShop(); shop.PowerUpSelect(2); });
        GUILayout.EndHorizontal();
        GUI.enabled = true;

        GUILayout.EndScrollView();
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    // A handler that throws inside OnGUI corrupts IMGUI's layout state for the rest of the
    // frame ("Invalid GUILayout state" spam). Log and keep the panel alive instead.
    private static void TryRun(Action action)
    {
        try { action(); }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    private void LoadRoom(NodeTypeEnum nodeType, RoomType tier)
    {
        if (SceneController.Instance == null)
        {
            Log("SceneController not found - start from the Map scene to load rooms.");
            return;
        }

        if (SceneManager.GetSceneByName(RoomSceneName).isLoaded)
        {
            PauseManager.SetPaused(false);
            SceneController.Instance.UnloadLevel();
        }

        Log($"loading {nodeType} room, tier {tier}");
        SceneController.Instance.LoadLevel(nodeType, tier);
    }

    private void ReturnToMap()
    {
        PauseManager.SetPaused(false);
        if (SceneController.Instance != null && SceneManager.GetSceneByName(RoomSceneName).isLoaded)
        {
            Log("returning to map");
            SceneController.Instance.UnloadLevel();
        }
        else
        {
            Log("no room loaded - nothing to return from");
        }
    }

    private void KillAllEnemies()
    {
        int killed = 0;
        Health[] all = FindObjectsByType<Health>(FindObjectsSortMode.None);
        foreach (Health h in all)
        {
            if (h != null && h.CompareTag("Enemy"))
            {
                h.TakeDamage(999999f);
                killed++;
            }
        }
        Debug.Log($"[DebugCheats] Killed {killed} enemies.");
    }

    private static void Log(string message)
    {
        Debug.Log($"[DebugCheats] {message}");
    }
}
#endif
