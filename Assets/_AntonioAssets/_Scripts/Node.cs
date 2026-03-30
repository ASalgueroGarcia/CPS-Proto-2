using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Node : MonoBehaviour
{
    [Header("Node Settings")] 
    [SerializeField] private NodeTypeEnum type;
    [SerializeField] private GameObject lineHolder;
    [SerializeField] private int nodeIndex;

    [Header("References")] 
    [SerializeField] private LineBetweenObjects line;
    [SerializeField] private bool generateType;

    private readonly List<Node> _parentNodes = new List<Node>();
    private readonly List<Node> _childNodes = new List<Node>();

    private readonly float _min = 0;
    private readonly float _max = 100;
    private bool _isActive = false;
    private bool _isStartingNode = false;
    private bool _isEndingNode = false;

    private LineBetweenObjects _lbo;
    private readonly HashSet<Node> _connectedChildren = new HashSet<Node>();

    private void Awake()
    {
        lineHolder = GameObject.Find("LineHolder");
        GenerateNodeType();
    }

    public void ResetNode()
    {
        _isActive = false;
        _isStartingNode = false;
        _isEndingNode = false;
        _connectedChildren.Clear();
        _parentNodes.Clear();
        _childNodes.Clear();

        foreach (Transform child in transform)
        {
            if (child != null)
                Destroy(child.gameObject);
        }

        GenerateNodeType();
    }

    private void GenerateNodeType()
    {
        if (!generateType) return;
        type = Random.Range(_min, _max + 1) <= 75 ? NodeTypeEnum.Combat : NodeTypeEnum.Merchant;

        //Debug.Log(this.gameObject.name + type);
    }
    
    public void ToggleNode()
    {
        gameObject.SetActive(_isActive);

        if (!_isActive || _childNodes == null) return;

        foreach (var child in _childNodes)
        {
            if (!child._isActive || _connectedChildren.Contains(child)) continue;

            var connectingLine = Instantiate(line, lineHolder.transform);
            connectingLine.SetObjects(this.gameObject, child.gameObject);
            _connectedChildren.Add(child);
        }
    }
    
    public void ActivateChildren(Node endingNode = null)
    {
        Debug.Log($"Node: {gameObject.name} has {_childNodes.Count} children");

        if (_childNodes.Count == 0)
        {
            Debug.LogWarning($"No children found on node {gameObject.name}!");
            return;
        }

        foreach (var child in _childNodes)
        {
            // If this child is the ending node, enable it via its own button
            if (endingNode != null && child == endingNode)
            {
                var bossBtn = child.GetComponentInChildren<Button>();
                if (bossBtn != null) bossBtn.interactable = true;
                continue;
            }

            var btn = child.GetComponentInChildren<Button>();
            if (btn != null) btn.interactable = true;
        }
    }

    public NodeTypeEnum GetNodeType()
    {
        return type;
    }

    public Transform GetNodeTransform()
    {
        return this.gameObject.transform;
    }

    public void SetChildNode(Node childNode)
    {
        if (childNode == null) return;
        if (!_childNodes.Contains(childNode)) _childNodes.Add(childNode);
        if (!childNode._parentNodes.Contains(this)) childNode._parentNodes.Add(this);
    }

    public void SetParentNode(Node parentNode)
    {
        if (parentNode == null) return;
        if (!_parentNodes.Contains(parentNode)) _parentNodes.Add(parentNode);
        if (!parentNode._childNodes.Contains(this)) parentNode._childNodes.Add(this);
    }

    public void SetNodeAsActive()
    {
        _isActive = true;
    }

    public void SetStartingNode()
    {
        _isStartingNode = true;
    }

    public void SetEndingNode()
    {
        _isEndingNode = true;
    }

    public bool IsStartingNode()
    {
        return _isStartingNode;
    }

    public bool IsEndingNode()
    {
        return _isEndingNode;
    }

    public bool HasNode(Node node)
    {
        return _childNodes.Contains(node);
    }

    public void SetType(NodeTypeEnum nodeType)
    {
        type = nodeType;
    }

    public void SetNodeIndex(int index)
    {
        nodeIndex = index;
    }

    public int GetNodeIndex()
    {
        return nodeIndex;
    }
}