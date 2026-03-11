using UnityEngine;

public class Node : MonoBehaviour
{
    private Node _parentNode;
    private Node _childNode;
    private int _nodeIndex;

    public void SetChildNode(Node childNode)
    {
        childNode.SetParentNode(this);

        _childNode = childNode;
    }

    public void SetParentNode(Node parentNode)
    {
        _parentNode = parentNode;
    }

    public int GetNodeIndex()
    {
        return _nodeIndex;
    }
}
