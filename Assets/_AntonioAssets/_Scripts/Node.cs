using System.Collections.Generic;
using UnityEngine;

public class Node : MonoBehaviour
{
    [Header("Node Settings")] 
    [SerializeField] private NodeTypeEnum type;

    [Header("References")]
    [SerializeField] private LineBetweenObjects line;
    [SerializeField] private Canvas canvas;
    [SerializeField] private bool generateType;
    
    private readonly List<Node> _parentNodes = new List<Node>();
    private readonly List<Node> _childNodes = new List<Node>();

    private readonly float _min = 0;
    private readonly float _max= 100;
    private bool _isActive = false;
    private bool _isStartingNode = false;

    private LineBetweenObjects _lbo;
    private readonly HashSet<Node> _connectedChildren = new HashSet<Node>();

    private void Awake()
    {
        canvas = FindObjectOfType<Canvas>();
        GenerateNodeType();
    }
    
    public void ResetNode()
    {
        _isActive = false;
        _isStartingNode = false;
        _connectedChildren.Clear();
        _parentNodes.Clear();
        _childNodes.Clear();
    
        foreach (Transform child in transform)
        {
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

    public bool IsStartingNode()
    {
        return _isStartingNode;
    }

    public void ToggleNode()
    {
        gameObject.SetActive(_isActive);

        if (!_isActive || _childNodes == null) return;

        foreach (var child in _childNodes)
        {
            if (!child._isActive || _connectedChildren.Contains(child)) continue;
        
            var connectingLine = Instantiate(line, canvas.transform);
            connectingLine.SetObjects(this.gameObject, child.gameObject);
            _connectedChildren.Add(child);
        }
    }

    public bool HasNode(Node node)
    {
        return _childNodes.Contains(node);
    }

    public void SetType(NodeTypeEnum nodeType)
    {
        type = nodeType;
    }
}