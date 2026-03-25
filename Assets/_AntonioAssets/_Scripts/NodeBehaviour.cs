using UnityEngine;

public class NodeBehaviour : MonoBehaviour
{
    public void LoadLevel()
    {
        var sceneController = SceneController.Instance;
        var node = GetComponentInParent<Node>();

        Debug.Log($"Button object: {gameObject.name}, Parent: {transform.parent?.name}, Node: {node?.gameObject.name}, Type: {node?.GetNodeType()}", this);

        if (node) sceneController.LoadLevel(node.GetNodeType());
    }
}