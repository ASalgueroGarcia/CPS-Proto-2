using UnityEngine;

[System.Serializable]
public class NodeLayer
{
    public Node[] nodes;
}

public class PathGenerator : MonoBehaviour
{
    [Header("Map Variables")] [SerializeField]
    private int minLayerNodes;

    [SerializeField] private int maxLayerNodes;
    [SerializeField] private int maxLayers;
    [SerializeField] private int pathNum;

    [Header("Node Map")] [SerializeField] private NodeLayer[] nodeLayers; // Assign in Inspector

    private int _startCoord = 0;

    private Node _startingNode;
    private Node _currNode;
    private Node _prevNode;
    private Node _nextNode;

    private Node[][] _map;

    private void Start()
    {
        // Convert the serialized NodeLayer[] into the Node[][] _map
        _map = new Node[nodeLayers.Length][];

        for (var i = 0; i < nodeLayers.Length; i++)
        {
            _map[i] = nodeLayers[i].nodes;
        }

        for (var i = 0; i < pathNum; i++)
        {
            GeneratePath();
        }
    }

    private void ChooseStartingNode()
    {
        _startCoord = (int)Random.Range(minLayerNodes, maxLayerNodes);

        _startingNode = _map[0][_startCoord];
        _currNode = _startingNode;

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

    private void GeneratePath()
    {
        ChooseStartingNode();
        ConnectNextNode(0, _startCoord);
        SetActiveNodes();
    }

    private void SetActiveNodes()
    {
        for (var i = 0; i < maxLayers; i++)
        {
            for (var j = 0; j < _map[i].Length; j++)
            {
                _map[i][j].ToggleNode();
                //Debug.Log($"Node: [{i},{j}] is {_map[i][j].isActiveAndEnabled}.");
            }
        }
    }
}