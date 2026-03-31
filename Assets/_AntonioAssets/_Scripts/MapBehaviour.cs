using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

[System.Serializable]
public class NodeLayer
{
    public Node[] nodes;
}

public class MapBehaviour : MonoBehaviour
{
    [Header("Map Variables")]
    [SerializeField] private int maxLayerNodes;
    [SerializeField] private int maxLayers;
    [SerializeField] private int pathNum;

    [Header("Node Map")] 
    [SerializeField] private NodeLayer[] nodeLayers;

    [Header("Node Prefabs")] 
    [SerializeField] private GameObject combatNode;
    [SerializeField] private GameObject merchantNode;
    [SerializeField] private GameObject bossNode;
    [SerializeField] private GameObject placeholderNode;
    //[SerializeField] private GameObject miniBossNode;
    //[SerializeField] private GameObject treasureNode;
    
    [Header("Node References")]
    [SerializeField] private Node finalNode;

    private int _startCoord = 0;
    private bool _isBossConnected = false;
    private int _currNodeIndex;

    private Node _startingNode;
    private Node _completedNode;
    private Node _currNode;
    private Node _prevNode;
    private Node _nextNode;
    private Node[][] _map;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "_MapScene") return;
        
        //Debug.Log("Scene Loaded");
        
        CreateMap();
    }

    private void CreateMap()
    {
        InitializeMap();

        for (var i = 0; i < pathNum; i++)
        {
            GeneratePath(); 
        } 
        
        SetActiveNodes();
        AssignLocations();
        MakeStartingNodesInteractable();
    }

    private void InitializeMap()
    {
        _map = new Node[nodeLayers.Length][];
        
        for (var i = 0; i < nodeLayers.Length; i++)
        {
            _map[i] = nodeLayers[i].nodes;
        }
    }

    private void ResetMap()
    {
        _isBossConnected = false;
        _startingNode = null;
        _currNode = null;
        _prevNode = null;
        _nextNode = null;

        if (finalNode) finalNode.ResetNode();

        foreach (var t in _map)
        {
            foreach (var t1 in t)
            {
                t1.ResetNode();
            }
        }
    }

    private void AssignLocations()
    {
        for (var l = 0; l < maxLayers; l++)
        {
            for (var n = 0; n < maxLayerNodes; n++)
            {
                var node = _map[l][n];
                SpawnNode(node.GetNodeType(), node.GetNodeTransform());
            }
        }
    }

    private void SpawnNode(NodeTypeEnum nodeType, Transform nodeTransform)
    {
        var node = nodeTransform.GetComponent<Node>();
        node.SetType(nodeType);

        var spawnedNode = nodeType switch
        {
            NodeTypeEnum.Combat => Instantiate(combatNode, nodeTransform, false),
            NodeTypeEnum.Merchant => Instantiate(merchantNode, nodeTransform, false),
            NodeTypeEnum.Placeholder => Instantiate(placeholderNode, nodeTransform, false),
            NodeTypeEnum.Boss => Instantiate(combatNode, nodeTransform, false),
            _ => throw new ArgumentOutOfRangeException(nameof(nodeType), nodeType, null)
        };

        var nodeBehaviour = spawnedNode.GetComponent<NodeBehaviour>();
        var button = spawnedNode.GetComponentInChildren<Button>();

        if (nodeBehaviour != null) nodeBehaviour.SetNode(node); // ← this is the key line
        if (button && nodeBehaviour) button.onClick.AddListener(nodeBehaviour.LoadLevel);
    }

    private void ChooseStartingNode() 
    { 
        _startCoord = Random.Range(0, _map[0].Length); 
        _startingNode = _map[0][_startCoord];

        if (!_startingNode)
        {
            Debug.Log("No Starting node found");
            ResetMap();
            InitializeMap();
            return;
        }
        
        if (!_startingNode.IsStartingNode())
        {
            _startingNode.SetStartingNode(); 
            _startingNode.SetType(NodeTypeEnum.Combat); 
            _currNode = _startingNode;
        }
        else
        {
            ChooseStartingNode();
        } 
        
        //Debug.Log("Starting node: [0," + _startCoord + "]");
    }

    private void ConnectNextNode(int layerIndex, int nodeIndex)
    {
        _currNode.SetParentNode(_currNode == _startingNode ? null : _prevNode);
        _currNode.SetNodeAsActive();

        var nextLayerIndex = layerIndex + 1;
        if (nextLayerIndex >= maxLayers) return;
        
        var minIndex = Mathf.Clamp(nodeIndex - 1, 0, _map[nextLayerIndex].Length - 1);
        var maxIndex = Mathf.Clamp(nodeIndex + 1, 0, _map[nextLayerIndex].Length - 1);
        var nextIndex = Random.Range(minIndex, maxIndex + 1);

        _nextNode = _map[nextLayerIndex][nextIndex];

        if (_currNode.HasNode(_nextNode))
        {
            minIndex = Mathf.Clamp(nodeIndex - 1, 0, _map[nextLayerIndex].Length - 1);
            maxIndex = Mathf.Clamp(nodeIndex + 1, 0, _map[nextLayerIndex].Length - 1);
            nextIndex = Random.Range(minIndex, maxIndex + 1);

            _nextNode = _map[nextLayerIndex][nextIndex];
        }

        _currNode.SetChildNode(_nextNode);
        //Debug.Log($"Moving from [{layerIndex},{nodeIndex}] to [{nextLayerIndex},{nextIndex}]");

        _prevNode = _currNode;
        _currNode = _nextNode;
        _nextNode = null;

        ConnectNextNode(nextLayerIndex, nextIndex);
    }
    
    private void ConnectToBossNode()
    {
        if (_isBossConnected) return;
    
        for (var i = 0; i < maxLayerNodes; i++)
        {
            _map[maxLayers-1][i].SetChildNode(finalNode);
        }

        finalNode.SetEndingNode();
        finalNode.SetType(NodeTypeEnum.Boss);

        var spawnedBoss = Instantiate(bossNode, finalNode.GetNodeTransform());
    
        var nodeBehaviour = spawnedBoss.GetComponent<NodeBehaviour>();
        var button = spawnedBoss.GetComponentInChildren<Button>();

        if (nodeBehaviour != null) nodeBehaviour.SetNode(finalNode);
        if (button != null && nodeBehaviour != null) button.onClick.AddListener(nodeBehaviour.LoadLevel);

        finalNode.SetNodeAsActive(); 
        finalNode.ToggleNode();
        _isBossConnected = true;
    }

    private void SetActiveNodes()
    {
        for (var i = 0; i < maxLayers; i++)
        {
            for (var j = 0; j < _map[i].Length; j++)
            {
                _map[i][j].ToggleNode();
            }
        }
    }
    
    private void MakeStartingNodesInteractable()
    {
        for (var i = 0; i < maxLayers; i++)
        {
            for (var j = 0; j < _map[i].Length; j++)
            {
                var node = _map[i][j];
                var nodeBtn = node.GetComponentInChildren<Button>();
                nodeBtn.interactable = nodeBtn && node.IsStartingNode();
            }
        }

        // Boss node starts locked
        var bossBtn = finalNode.GetComponentInChildren<Button>();
        if (bossBtn != null) bossBtn.interactable = false;
    }
    
    private void GeneratePath()
    {
        ChooseStartingNode();
        ConnectNextNode(0, _startCoord);
        ConnectToBossNode();
    }

    public void CompletedNode()
    {
        if (_completedNode == null)
        {
            Debug.LogError("CompletedNode called but _completedNode is null!");
            return;
        }

        // Lock all buttons first
        for (var i = 0; i < maxLayers; i++)
        {
            for (var j = 0; j < _map[i].Length; j++)
            {
                var btn = _map[i][j].GetComponentInChildren<Button>();
                if (btn != null) btn.interactable = false;
            }
        }

        // Also lock boss button
        var bossBtn = finalNode.GetComponentInChildren<Button>();
        if (bossBtn != null) bossBtn.interactable = false;

        // Unlock children — if a child is the ending node, enable the boss button instead
        _completedNode.ActivateChildren(finalNode);
        _completedNode = null;
    }

    public void SetCurrentNodeIndex(int index)
    {
        _currNodeIndex = index;
    }
    
    public void SetCurrentNode(Node node)
    {
        _completedNode = node;
        //Debug.Log($"Current node set to: {node.gameObject.name}");
    }
}