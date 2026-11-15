using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class MerchantBehaviour : MonoBehaviour
{
    [Header("Merchant Settings")]
    [SerializeField] private float popUpRadius = 5f;
    [SerializeField] private GameObject dialoguePrompt;

    [Header("Player Settings")]
    [SerializeField] private Player player;

    void Awake()
    {
        if (dialoguePrompt) dialoguePrompt.SetActive(false);
    }

    void OnValidate()
    {
        GetComponent<SphereCollider>().radius = popUpRadius;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        //player.InteractibleInRange = true;

        EnableDialoguePrompt();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        dialoguePrompt.SetActive(false);
    }

    private void EnableDialoguePrompt()
    {
        dialoguePrompt.SetActive(true);
        transform.forward = Camera.main.transform.forward;
    }
}
