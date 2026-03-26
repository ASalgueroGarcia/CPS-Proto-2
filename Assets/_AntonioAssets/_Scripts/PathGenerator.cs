using System;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

[System.Serializable]
public class NodeLayer
{
    public Node[] nodes;
}

public class PathGenerator : MonoBehaviour
{
    [Header("Map Variables")] 
    [SerializeField] private int minLayerNodes;
    [SerializeField] private int maxLayerNodes;
    [SerializeField] private int maxLayers;
    [SerializeField] private int pathNum;

    [Header("Node Map")] 
    [SerializeField] private NodeLayer[] nodeLayers;

    [Header("Node Prefabs")] 
    [SerializeField] private GameObject combatNode;
    [SerializeField] private GameObject merchantNode;
    [SerializeField] private GameObject bossNode;
    //[SerializeField] private GameObject placeholderNode;
    //[SerializeField] private GameObject miniBossNode;
    //[SerializeField] private GameObject treasureNode;
    
    [Header("Boss Node")]
    [SerializeField] private Node finalNode;

    private int _startCoord = 0;
    private bool _hasBeenGenerated = false;
    private bool _isBossConnected = false;

    private Node _startingNode;
    private Node _currNode;
    private Node _prevNode;
    private Node _nextNode;
    private Node[][] _map;

    
    public static PathGenerator Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Convert the serialized NodeLayer[] into the Node[][] _map
        _map = new Node[nodeLayers.Length][];

        for (var i = 0; i < nodeLayers.Length; i++)
        {
            _map[i] = nodeLayers[i].nodes;
        }
    
        if(_hasBeenGenerated) return;

        CreateMap();
    }

    private void CreateMap()
    {
        for (var i = 0; i < pathNum; i++)
        {
            GeneratePath();
        }

        AssignLocations();
    }

    public void ResetMap()
    {
        _hasBeenGenerated = false;
        _isBossConnected = false;
        _startingNode = null;
        _currNode = null;
        _prevNode = null;
        _nextNode = null;

        if (finalNode) finalNode.ResetNode();

        for (var i = 0; i < _map.Length; i++)
        {
            for (var j = 0; j < _map[i].Length; j++)
            {
                _map[i][j].ResetNode();
            }
        }

        CreateMap();
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
        nodeTransform.GetComponent<Node>().SetType(nodeType);
        GameObject spawnedNode = null;

        switch (nodeType)
        {
            case NodeTypeEnum.Combat:
                spawnedNode = Instantiate(combatNode, nodeTransform, false);
                break;
            case NodeTypeEnum.Merchant:
                spawnedNode = Instantiate(merchantNode, nodeTransform, false);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(nodeType), nodeType, null);
        }

        var nodeBehaviour = spawnedNode.GetComponent<NodeBehaviour>();
        var button = spawnedNode.GetComponentInChildren<Button>();

        if (button && nodeBehaviour) button.onClick.AddListener(nodeBehaviour.LoadLevel);
    }

    private void ChooseStartingNode()
    {
        _startCoord = (int)Random.Range(minLayerNodes, maxLayerNodes);
        _startingNode = _map[0][_startCoord];

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

        Instantiate(bossNode, finalNode.GetNodeTransform());
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
                var node = _map[i][j];
                node.ToggleNode();
            }
        }
    }
    
    private void GeneratePath()
    {
        ChooseStartingNode();
        ConnectNextNode(0, _startCoord);
        ConnectToBossNode();
        SetActiveNodes();
        _hasBeenGenerated = true;
    }
}