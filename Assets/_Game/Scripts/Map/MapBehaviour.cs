using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class MapBehaviour : MonoBehaviour
{
    private enum NodeState
    {
        Current,
        Available,
        Later,
        Closed
    }

    [Header("Layout")]
    [SerializeField] private RectTransform nodeArea;
    [SerializeField] private RectTransform lineHolder;
    [SerializeField] private GameObject nodePrefab;
    [SerializeField] private LineBetweenObjects linePrefab;
    [SerializeField] private float nodeSize = 64f;
    [SerializeField] private Vector2 areaPadding = new Vector2(0.15f, 0.18f);

    [Header("Rooms")]
    [SerializeField] private RoomType[] rowTiers;
    [Range(0f, 1f)] [SerializeField] private float restRoomChance = 0.25f;
    [Range(0f, 1f)] [SerializeField] private float shopRoomChance = 0.15f;
    [SerializeField] private float restRoomHeal = 40f;

    [Header("Node Rewards")]
    [Range(0f, 1f)] [SerializeField] private float moreHealthDropChance = 0.45f;
    [SerializeField] private float moreButtonsMultiplier = 1.5f;
    [SerializeField] private float treasureHeal = 50f;
    [SerializeField] private int[] treasureButtons = { 10, 12, 14 };

    [Header("Colors")]
    [SerializeField] private Color gold = new Color(1f, 0.78f, 0.22f);
    [SerializeField] private Color grey = new Color(0.55f, 0.55f, 0.55f);
    [SerializeField] private Color dimmed = new Color(0.3f, 0.3f, 0.3f, 0.35f);

    private MapGrid map;
    private readonly Node[,] nodes = new Node[MapGrid.Rows, MapGrid.Columns];
    private readonly List<(Image image, int row, int from, int to)> lines = new List<(Image, int, int, int)>();
    private readonly List<int> path = new List<int>();

    private TMP_Text header;
    private TMP_Text hud;
    private TMP_Text inspect;

    private Node entered;
    private readonly List<(RoomType room, Action<WaveManager> setup)> rooms = new List<(RoomType, Action<WaveManager>)>();
    private int roomIndex;
    private bool roomCompleted;
    private const int MaxShopsPerScene = 2;
    private int shopRoomCount;

    public bool HasNextRoom => entered != null && entered.Type != NodeTypeEnum.FinalFight
                               && roomCompleted && roomIndex + 1 < rooms.Count;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        WaveManager.OnEnemiesCleared += OnEnemiesCleared;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        WaveManager.OnEnemiesCleared -= OnEnemiesCleared;
    }

    private void Start()
    {
        if (map == null) CreateMap();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "_MapScene") return;
        CreateMap();
    }

    private void OnEnemiesCleared()
    {
        roomCompleted = true;
        if (entered != null && entered.Type == NodeTypeEnum.FinalFight)
        {
            Services.Run?.EndRun(RunOutcome.Victory);
        }
    }

    private void CreateMap()
    {
        Time.timeScale = 1f;
        if (ScreenFader.Instance != null) ScreenFader.Instance.Set(0f);
        Clear();

        map = MapGenerator.Generate(new System.Random(Random.Range(int.MinValue, int.MaxValue)));
        BuildHud();
        PlaceLineHolder();

        for (int row = 0; row < MapGrid.Rows; row++)
        {
            for (int column = 0; column < MapGrid.Columns; column++)
            {
                if (map.IsNode(row, column))
                {
                    SpawnNode(row, column);
                }
            }
        }

        for (int row = 0; row < MapGrid.LastRow; row++)
        {
            foreach (var (from, to) in map.EdgesFrom(row))
            {
                SpawnLine(row, from, to);
            }
        }

        Refresh();
    }

    private void Clear()
    {
        foreach (Node node in nodes)
        {
            if (node != null) Destroy(node.gameObject);
        }
        Array.Clear(nodes, 0, nodes.Length);

        foreach (var line in lines)
        {
            if (line.image != null) Destroy(line.image.gameObject);
        }
        lines.Clear();

        path.Clear();
        entered = null;
        shopRoomCount = 0;
    }

    private void PlaceLineHolder()
    {
        lineHolder.SetParent(nodeArea, false);
        lineHolder.SetAsFirstSibling();
        lineHolder.anchorMin = Vector2.zero;
        lineHolder.anchorMax = Vector2.one;
        lineHolder.offsetMin = Vector2.zero;
        lineHolder.offsetMax = Vector2.zero;
        lineHolder.pivot = nodeArea.pivot;
    }

    private void SpawnNode(int row, int column)
    {
        RectTransform rect = Instantiate(nodePrefab, nodeArea, false).GetComponent<RectTransform>();
        bool singleNodeRow = row == 0 || row == MapGrid.LastRow;
        float u = singleNodeRow ? 0.5f : (column + 0.5f) / MapGrid.Columns;
        float v = (float)row / MapGrid.LastRow;
        Vector2 anchor = new Vector2(Mathf.Lerp(areaPadding.x, 1f - areaPadding.x, u), Mathf.Lerp(areaPadding.y, 1f - areaPadding.y, v));

        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.one * nodeSize;
        rect.name = $"Node_{row}_{column}";

        Node node = rect.GetComponent<Node>();
        node.Init(row, column, map.TypeAt(row, column), Enter, Inspect);
        nodes[row, column] = node;
    }

    private void SpawnLine(int row, int from, int to)
    {
        LineBetweenObjects line = Instantiate(linePrefab, lineHolder, false);
        line.SetObjects(nodes[row, from].gameObject, nodes[row + 1, to].gameObject);
        lines.Add((line.GetComponent<Image>(), row, from, to));
    }

    private void Refresh()
    {
        HashSet<(int, int)> available = new HashSet<(int, int)>();
        if (path.Count == 0)
        {
            available.Add((0, 0));
        }
        else
        {
            int row = path.Count - 1;
            foreach (int child in map.Children(row, path[row]))
            {
                available.Add((row + 1, child));
            }
        }

        HashSet<(int, int)> reachable = new HashSet<(int, int)>(available);
        for (int row = path.Count; row < MapGrid.LastRow; row++)
        {
            foreach (var (from, to) in map.EdgesFrom(row))
            {
                if (reachable.Contains((row, from)))
                {
                    reachable.Add((row + 1, to));
                }
            }
        }

        Node firstAvailable = null;
        for (int row = 0; row < MapGrid.Rows; row++)
        {
            for (int column = 0; column < MapGrid.Columns; column++)
            {
                Node node = nodes[row, column];
                if (node == null) continue;

                NodeState state = OnPath(row, column) ? (row == path.Count - 1 ? NodeState.Current : NodeState.Closed)
                    : available.Contains((row, column)) ? NodeState.Available
                    : reachable.Contains((row, column)) ? NodeState.Later
                    : NodeState.Closed;

                ApplyState(node, state);
                if (state == NodeState.Available && firstAvailable == null)
                {
                    firstAvailable = node;
                }
            }
        }

        foreach (var (image, row, from, to) in lines)
        {
            bool traversed = OnPath(row, from) && OnPath(row + 1, to);
            bool ahead = (OnPath(row, from) || reachable.Contains((row, from))) && reachable.Contains((row + 1, to));
            image.color = traversed ? gold : ahead ? grey : dimmed;
        }

        RefreshHud();

        if (firstAvailable != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(firstAvailable.gameObject);
        }
    }

    private bool OnPath(int row, int column)
    {
        return row < path.Count && path[row] == column;
    }

    private void ApplyState(Node node, NodeState state)
    {
        Color roomColor = Node.GetRoomColor(node.Type);

        switch (state)
        {
            case NodeState.Current:
                node.SetLook(roomColor, Color.white, gold, showRing: true, interactable: false, current: true);
                break;
            case NodeState.Available:
                node.SetLook(roomColor, Color.white, Color.white, showRing: true, interactable: true, current: false);
                break;
            case NodeState.Later:
                Color laterFill = new Color(roomColor.r * 0.45f, roomColor.g * 0.45f, roomColor.b * 0.45f, 0.95f);
                node.SetLook(laterFill, roomColor, roomColor, showRing: true, interactable: false, current: false);
                break;
            default:
                Color closedFill = new Color(roomColor.r * 0.2f, roomColor.g * 0.2f, roomColor.b * 0.2f, 0.3f);
                Color closedIcon = new Color(roomColor.r * 0.5f, roomColor.g * 0.5f, roomColor.b * 0.5f, 0.3f);
                node.SetLook(closedFill, closedIcon, Color.clear, showRing: false, interactable: false, current: false);
                break;
        }
    }

    private void Enter(Node node)
    {
        if (entered != null) return;

        entered = node;
        path.Add(node.Column);
        PlanRooms(node);
        roomIndex = 0;
        LoadRoom();
    }

    private void LoadRoom()
    {
        var (room, setup) = rooms[roomIndex];
        roomCompleted = room == RoomType.Shop || room == RoomType.Treasure;
        SceneController.Instance.LoadLevel(room, setup, entered.Row, roomIndex == 0);
    }

    public void LeaveRoom()
    {
        if (entered == null)
        {
            SceneController.Instance.UnloadLevel();
            return;
        }

        if (entered.Type == NodeTypeEnum.FinalFight)
        {
            bool won = roomCompleted;
            entered = null;
            if (won)
            {
                Services.Flow?.EnterHub();
                return;
            }

            path.RemoveAt(path.Count - 1);
            SceneController.Instance.UnloadLevel();
            Refresh();
            return;
        }

        if (roomCompleted && ++roomIndex < rooms.Count)
        {
            LoadRoom();
            return;
        }

        // Leaving before the node's last room is cleared (pause > Return to map) doesn't pass the
        // node: it stays open on the map, like an unfinished Final Fight.
        if (!roomCompleted) path.RemoveAt(path.Count - 1);

        entered = null;
        SceneController.Instance.UnloadLevel();
        Refresh();
    }

    private void PlanRooms(Node node)
    {
        rooms.Clear();
        switch (node.Type)
        {
            case NodeTypeEnum.Shop:
                shopRoomCount++;
                rooms.Add((RoomType.Shop, null));
                return;
            case NodeTypeEnum.Treasure:
                int buttons = treasureButtons[Mathf.Clamp(SceneIndex(), 0, treasureButtons.Length - 1)];
                if (Random.value < 0.5f)
                {
                    rooms.Add((RoomType.Treasure, wm => wm.SetRewards(currency: 0, healthDropChance: 1f, healAmount: treasureHeal)));
                }
                else
                {
                    rooms.Add((RoomType.Treasure, wm => wm.SetRewards(currency: buttons, healthDropChance: 0f)));
                }
                return;
            case NodeTypeEnum.FinalFight:
                rooms.Add((SceneIndex() >= ProgressRules.ScenesPerAct - 1 ? RoomType.Boss : RoomType.MiniBoss, null));
                return;
        }

        Action<WaveManager> rewards = node.Type switch
        {
            NodeTypeEnum.MoreHealth => wm => wm.SetRewards(healthDropChance: moreHealthDropChance),
            NodeTypeEnum.MoreButtons => wm => wm.SetRewards(currencyMultiplier: moreButtonsMultiplier),
            _ => null
        };

        int count = Random.value < 0.5f ? 4 : 5;
        RoomType tier = BaseTier(node.Row);
        for (int i = 0; i < count; i++)
        {
            rooms.Add((node.Row == 0 ? tier : StepUp(tier, i / 2), rewards));
        }

        if (node.Row == 0) return;

        float roll = Random.value;
        int middle = Random.Range(1, count - 1);
        if (roll < restRoomChance)
        {
            rooms[middle] = (RoomType.Treasure, wm => wm.SetRewards(currency: 0, healthDropChance: 1f, healAmount: restRoomHeal));
        }
        else if (shopRoomCount < MaxShopsPerScene && roll < restRoomChance + shopRoomChance)
        {
            shopRoomCount++;
            rooms[middle] = (RoomType.Shop, null);
        }
    }

    private RoomType BaseTier(int row)
    {
        if (rowTiers != null && row < rowTiers.Length) return rowTiers[row];
        if (row <= 2) return RoomType.Entrance;
        return row <= 4 ? RoomType.Medium : RoomType.MediumHard;
    }

    private static RoomType StepUp(RoomType tier, int steps)
    {
        return (RoomType)Mathf.Min((int)tier + steps, (int)RoomType.Hard);
    }

    private static int SceneIndex()
    {
        return Services.Run != null && Services.Run.IsActive ? Services.Run.Scene : 0;
    }

    private void BuildHud()
    {
        if (header != null) return;

        Transform canvas = nodeArea.GetComponentInParent<Canvas>().transform;
        header = MakeText(canvas, "Header", new Vector2(0f, 1f), TextAlignmentOptions.TopLeft, 34f);
        hud = MakeText(canvas, "Hud", new Vector2(1f, 1f), TextAlignmentOptions.TopRight, 34f);
        inspect = MakeText(canvas, "Inspect", new Vector2(0.5f, 0f), TextAlignmentOptions.Bottom, 30f);
        inspect.rectTransform.anchoredPosition += Vector2.up * 70f;

        TMP_Text legend = MakeText(canvas, "Legend", new Vector2(0.5f, 0f), TextAlignmentOptions.Bottom, 22f);
        legend.color = grey;
        legend.text = "Select a room to enter";
    }

    private void RefreshHud()
    {
        var run = Services.Run;
        bool active = run != null && run.IsActive;
        header.text = $"ACT {(active ? run.Act + 1 : 1)} · SCENE {(active ? run.Scene + 1 : 1)}\n<size=70%>Take {(active ? run.CurrentTake : 1)}</size>";

        var stats = PlayerStatsManager.Instance;
        string health = stats != null && stats.MaxHealth > 0f ? $"{stats.CurrentHealth:0} / {stats.MaxHealth:0}" : "-- / --";
        hud.text = $"HP {health}\n<size=70%>Buttons {(stats != null ? stats.CurrentCoins : 0)}</size>";
        inspect.text = "";
    }

    private void Inspect(Node node)
    {
        string description = node.Row == 0 ? "First Spot · 4-5 rooms" : node.Type switch
        {
            NodeTypeEnum.Combat => "Combat · 4-5 rooms",
            NodeTypeEnum.MoreHealth => "More Health · 4-5 rooms",
            NodeTypeEnum.MoreButtons => "More Buttons · 4-5 rooms",
            NodeTypeEnum.Shop => "Shop · merchant",
            NodeTypeEnum.Treasure => "Treasure · rewards",
            NodeTypeEnum.FinalFight => "Final Fight · Boss",
            _ => ""
        };
        inspect.text = description;
    }

    private static TMP_Text MakeText(Transform parent, string name, Vector2 corner, TextAlignmentOptions alignment, float size)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = corner;
        rect.anchorMax = corner;
        rect.pivot = corner;
        rect.anchoredPosition = new Vector2((0.5f - corner.x) * 80f, (0.5f - corner.y) * 60f);
        rect.sizeDelta = new Vector2(corner.x == 0.5f ? 1200f : 560f, 120f);

        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }
}
