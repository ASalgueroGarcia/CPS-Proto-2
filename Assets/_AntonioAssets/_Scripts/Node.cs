using System.Collections.Generic;
using UnityEngine;

public class Node : MonoBehaviour
{
    [Header("Node Settings")] 
    [SerializeField] private NodeTypeEnum type;
    
    private readonly List<Node> _parentNodes = new List<Node>();
    private readonly List<Node> _childNodes = new List<Node>();

    private readonly float _min = 0;
    private readonly float _max= 100;
    private bool _isActive = false;
    private bool _isStartingNode = false;

    private LineRenderer _lr;

    private void Awake()
    {
        _lr = gameObject.AddComponent<LineRenderer>();
        _lr.startColor = Color.red;
        _lr.endColor = Color.green;
        _lr.startWidth = 0.2f;
        _lr.endWidth = 0.2f;
        _lr.enabled = false;
        
        GetComponent<NodeBehaviour>().SetNode(this);
        
        GenerateNodeType();
    }

    private void GenerateNodeType()
    {
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

        _lr.positionCount = _childNodes.Count * 2;
        _lr.enabled = true;
        
        for (var i = 0; i < _childNodes.Count; i++)
        {
            _lr.SetPosition(i * 2, transform.position);
            _lr.SetPosition(i * 2 + 1, _childNodes[i].transform.position);
        }
    }

    public bool HasNode(Node node)
    {
        return _childNodes.Contains(node);
    }
}