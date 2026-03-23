using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class NodeBehaviour : MonoBehaviour
{
    [Header("Scene Management")] 
    [SerializeField] private SceneController sceneController;

    private Node _node;
    
    private float _min;
    
    private void Awake()
    {
        sceneController = FindObjectOfType<SceneController>();

        if (sceneController == null)
            Debug.LogError("No SceneController found in scene!", this);
    }
    
    private void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame) MousePressed();
    }

    public void SetNode(Node node)
    {
        _node = node;
    }

    private void MousePressed()
    {
        var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        Physics.Raycast(ray, out var hit);

        if (!hit.transform || hit.transform.gameObject != gameObject) return;

        Debug.Log($"sceneController: {sceneController}, _node: {_node}", this);

        sceneController.LoadLevel(_node.GetNodeType());
    }
}
