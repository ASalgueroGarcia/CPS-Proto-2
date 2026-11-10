using System;
using UnityEngine;

public class MerchantBehaviour : MonoBehaviour
{
    [Header("Merchant Settings")]
    [SerializeField] private float popUpRadius = 5f;
    [SerializeField] private GameObject dialoguePrompt;

    void Awake()
    {
        dialoguePrompt.SetActive(false);
    }

    // If you want to set this while playing, change the start to update
    void Start()
    {
        GetComponent<SphereCollider>().radius = popUpRadius;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            EnableDialoguePrompt();
        }
    }

    void OnTriggerExit(Collider other)
    {
        dialoguePrompt.SetActive(false);
    }

    private void EnableDialoguePrompt()
    {
        dialoguePrompt.SetActive(true);
        dialoguePrompt.transform.LookAt(Camera.main.transform);
    }
}
