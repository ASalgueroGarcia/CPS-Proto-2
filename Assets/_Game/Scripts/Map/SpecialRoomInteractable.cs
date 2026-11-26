using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpecialRoomInteractable : MonoBehaviour
{
    public static event Action<SpecialRoomInteractable> OnDealAccepted;
    public static event Action<SpecialRoomInteractable> OnNarrativeTriggered;

    [Header("Special Room Settings")]
    [SerializeField] private SpecialRoomKind kind = SpecialRoomKind.Rest;
    [Min(0f)] [SerializeField] private float healAmount = 40f;
    [Min(0)] [SerializeField] private int buttonReward = 10;
    [Range(0f, 0.9f)] [SerializeField] private float dealHealthCostRatio = 0.2f;
    [Min(0f)] [SerializeField] private float dealWeaponDamageBonus = 5f;

    [Header("Visuals")]
    [SerializeField] private GameObject activeVisual;
    [SerializeField] private GameObject spentVisual;
    [SerializeField] private Renderer propRenderer;
    [SerializeField] private Color spentColor = new Color(0.32f, 0.32f, 0.32f, 1f);

    private bool isUsed;
    private bool playerInRange;
    private Health nearbyPlayerHealth;

    public SpecialRoomKind Kind => kind;
    public bool IsUsed => isUsed;
    public float HealAmount => healAmount;
    public int ButtonReward => buttonReward;
    public float DealHealthCostRatio => dealHealthCostRatio;
    public float DealWeaponDamageBonus => dealWeaponDamageBonus;

    public void Configure(SpecialRoomKind roomKind, RoomRewards? rewards = null)
    {
        kind = roomKind;
        isUsed = false;

        if (rewards.HasValue)
        {
            RoomRewards r = rewards.Value;
            if (r.HealAmount > 0f) healAmount = r.HealAmount;
            if (r.Currency >= 0) buttonReward = r.Currency;
        }
        else
        {
            healAmount = roomKind == SpecialRoomKind.Health ? 60f : roomKind == SpecialRoomKind.Treasure ? 50f : 40f;
            buttonReward = 0;
        }

        UpdateVisualState();
    }

    public void BindVisuals(GameObject activeObj, GameObject spentObj, Renderer renderer)
    {
        activeVisual = activeObj;
        spentVisual = spentObj;
        propRenderer = renderer;
        UpdateVisualState();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        nearbyPlayerHealth = other.GetComponent<Health>() ?? other.GetComponentInParent<Health>();
        Player.InteractableInRange = !isUsed;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        nearbyPlayerHealth = null;
        Player.InteractableInRange = false;
    }

    private void OnDisable()
    {
        if (playerInRange)
        {
            playerInRange = false;
            Player.InteractableInRange = false;
        }
    }

    private void Update()
    {
        if (!playerInRange || isUsed || Player.IsPaused) return;
        if (!WasInteractPressed()) return;

        TryInteract(nearbyPlayerHealth, PlayerStatsManager.Instance);
    }

    public bool TryInteract(Health playerHealth) => TryInteract(playerHealth, null);

    public bool TryInteract(Health playerHealth, PlayerStatsManager stats)
    {
        if (isUsed || kind == SpecialRoomKind.None) return false;

        switch (kind)
        {
            case SpecialRoomKind.Rest:
            case SpecialRoomKind.Health:
                ApplyHeal(healAmount, playerHealth, stats);
                break;

            case SpecialRoomKind.Treasure:
                if (buttonReward > 0)
                {
                    if (stats != null) stats.AddCoins(buttonReward);
                }
                else
                {
                    ApplyHeal(healAmount, playerHealth, stats);
                }
                break;

            case SpecialRoomKind.Deal:
                float currentHp = stats != null && stats.MaxHealth > 0f
                    ? stats.CurrentHealth
                    : playerHealth != null ? playerHealth.currentHealth : 0f;
                float cost = ComputeDealHealthCost(currentHp, dealHealthCostRatio);
                if (cost > 0f && playerHealth != null)
                {
                    playerHealth.SetHealth(Mathf.Max(1f, playerHealth.currentHealth - cost));
                }
                if (stats != null)
                {
                    stats.MoreNormalDamage(dealWeaponDamageBonus);
                }
                OnDealAccepted?.Invoke(this);
                break;

            case SpecialRoomKind.Narrative:
                OnNarrativeTriggered?.Invoke(this);
                break;
        }

        isUsed = true;
        Player.InteractableInRange = false;
        UpdateVisualState();
        return true;
    }

    public static float ComputeDealHealthCost(float currentHealth, float costRatio)
    {
        if (currentHealth <= 1f || costRatio <= 0f) return 0f;
        float raw = Mathf.Floor(currentHealth * Mathf.Clamp01(costRatio));
        return Mathf.Clamp(raw, 0f, currentHealth - 1f);
    }

    private static void ApplyHeal(float amount, Health playerHealth, PlayerStatsManager stats)
    {
        if (amount <= 0f) return;

        if (stats != null)
        {
            stats.Heal(amount);
        }
        else if (playerHealth != null)
        {
            playerHealth.SetHealth(playerHealth.currentHealth + amount);
        }
    }

    private void UpdateVisualState()
    {
        if (activeVisual != null) activeVisual.SetActive(!isUsed);
        if (spentVisual != null) spentVisual.SetActive(isUsed);

        if (isUsed && propRenderer != null && propRenderer.sharedMaterial != null)
        {
            var block = new MaterialPropertyBlock();
            propRenderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", spentColor);
            block.SetColor("_Color", spentColor);
            propRenderer.SetPropertyBlock(block);
        }
    }

    private static bool WasInteractPressed()
    {
        bool key = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        bool pad = Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame;
        return key || pad;
    }
}
