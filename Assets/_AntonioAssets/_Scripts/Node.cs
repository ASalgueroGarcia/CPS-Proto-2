using UnityEngine;

public class Node : MonoBehaviour
{
    private Node _parentNode;
    private Node _childNode;
    private bool _isActive = false;
    private LineRenderer _lr;

    private void Awake()
    {
        _lr = gameObject.AddComponent<LineRenderer>();
        _lr.startColor = Color.red;
        _lr.endColor = Color.green;
        _lr.startWidth = 0.2f;
        _lr.endWidth = 0.2f;
        _lr.positionCount = 2;
        _lr.enabled = false;
    }

    public void SetChildNode(Node childNode)
    {
        _childNode = childNode;
        _childNode.SetParentNode(this);
    }

    public void SetParentNode(Node parentNode)
    {
        _parentNode = parentNode;
    }

    public void SetNodeAsActive()
    {
        _isActive = true;
    }

    public void ToggleNode()
    {
        gameObject.SetActive(_isActive);

        if (!_isActive || _childNode == null) return;

        _lr.enabled = true;
        _lr.SetPosition(0, transform.position);
        _lr.SetPosition(1, _childNode.transform.position);
    }
}