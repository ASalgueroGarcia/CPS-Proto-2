using UnityEngine;

public class NodeBehaviour : MonoBehaviour
{
    private MapBehaviour _mapBehaviour;
    private Node _node;

    private void Awake()
    {
        _mapBehaviour = FindFirstObjectByType<MapBehaviour>();
    }

    public void SetNode(Node node)
    {
        _node = node;
        Debug.Log($"SetNode called on {gameObject.name}, assigned: {_node.gameObject.name}");
    }

    public void LoadLevel()
    {
        if (_node == null)
        {
            Debug.LogError($"No node reference on {gameObject.name}!", this);
            return;
        }

        //Debug.Log($"Loading: {_node.gameObject.name}, Type: {_node.GetNodeType()}");
        _mapBehaviour.SetCurrentNode(_node);
        SceneController.Instance.LoadLevel(_node.GetNodeType());
    }
}