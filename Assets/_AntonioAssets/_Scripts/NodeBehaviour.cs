using UnityEngine;

public class NodeBehaviour : MonoBehaviour
{
    private MapBehaviour _mapBehaviour;

    private void Start()
    {
        _mapBehaviour = FindFirstObjectByType<MapBehaviour>();
    }

    public void LoadLevel()
    {
        var sceneController = SceneController.Instance;
        var node = GetComponentInParent<Node>();
        
        /*
        Debug.Log($"Button object: {gameObject.name}, Parent: {transform.parent?.name}, " +
                  $"Node: {node?.gameObject.name}, Type: {node?.GetNodeType()}, " +
                  $"Index: {transform.parent?.GetComponent<Node>().GetNodeIndex()}", this);
        */

        if (!node) return;

        if (transform.parent) _mapBehaviour.SetCurrentNodeIndex(transform.parent.GetComponent<Node>().GetNodeIndex());
        sceneController.LoadLevel(node.GetNodeType());
    }
}