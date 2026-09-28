using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScissorsWeapon : MonoBehaviour, IWeapon
{
    [Header("Combat Stats")]
    public float weaponBaseDamage = 10f;
    public float baseCritChance = 0.05f;
    public float attackRange = 2.0f;
    public float specialRange = 5.0f;
    [SerializeField] private float knockBackForce = 7f;

    [Header("Combo")]
    [SerializeField] private float comboResetTime = 1.0f;
    [SerializeField] private float attackAnimationSpeed = 1.5f;
    [SerializeField] private float specialAttackAnimationSpeed = 1.2f;

    [Header("Special Attack")]
    public float specialCooldown = 10f;
    [SerializeField] private float specialKnockbackForce = 10f;

    [Header("Hitboxes (manual)")]
    [SerializeField] private GameObject generalAttackHitbox;
    [SerializeField] private GameObject hitboxAttack12;
    [SerializeField] private GameObject hitboxAttack3;

    [Header("Audio Clips (manual)")]
    [SerializeField] private List<AudioClip> lightAttackClips;
    [SerializeField] private AudioClip specialAttackClip;

    [Header("Debug")]
    [SerializeField] private bool showSpecialGizmo = false;

    private HitboxTrigger _generalTrigger;
    private HitboxTrigger _trigger12;
    private HitboxTrigger _trigger3;

    private List<Health> _hitEnemies = new List<Health>();
    private List<Breakable_Objects> _hitBreakables = new List<Breakable_Objects>();

    private float _lastAttackTime = 0f;
    private float _specialTimer = 0f;
    private float _fallbackTimer = 0f;
    private int _comboStep = 0;
    private bool _isComboWindowOpen = false;
    private bool _wasHit = false;
    private bool _hasActiveHitbox = false;
    private float _currentDamage;
    private float _currentCritChance;
    private float _currentKnockback;

    private Color _originalColor;

    private static readonly int Attack1Hash = Animator.StringToHash("Attack1");
    private static readonly int Attack2Hash = Animator.StringToHash("Attack2");
    private static readonly int Attack3Hash = Animator.StringToHash("Attack3");
    private static readonly int HeavyAttackHash = Animator.StringToHash("HeavyAttack");
    private const string HeavyAttackState = "HeavyAttack";

    private Player _player;

    // Accessors que delegan al Player (fuente única de verdad)
    private Animator Animator => _player != null ? _player.animator : null;
    private Renderer BodyRenderer => _player != null ? _player.bodyRenderer : null;
    private Health PlayerHealth => _player != null ? _player.playerHealth : null;
    private LayerMask EnemyLayer => _player != null ? _player.enemyLayer : 0;
    private Transform PlayerTransform => _player != null ? _player.transform : transform;

    public string Name => "Scissors";

    public float BaseDamage
    {
        get => weaponBaseDamage;
        set => weaponBaseDamage = value;
    }

    public float BaseCritChance
    {
        get => baseCritChance;
        set => baseCritChance = value;
    }

    public bool IsComboWindowOpen => _isComboWindowOpen;
    public float LastAttackTime => _lastAttackTime;
    public int CurrentComboStep => _comboStep;
    public bool HasActiveHitbox => _hasActiveHitbox;
    public float SpecialTimer => _specialTimer;
    public float AttackAnimationSpeed => attackAnimationSpeed;
    public float SpecialAnimationSpeed => specialAttackAnimationSpeed;

    public void Awake()
    {
        _generalTrigger = SetupHitbox(generalAttackHitbox);
        _trigger12 = SetupHitbox(hitboxAttack12);
        _trigger3 = SetupHitbox(hitboxAttack3);

        if (generalAttackHitbox != null) generalAttackHitbox.SetActive(false);
        if (hitboxAttack12 != null) hitboxAttack12.SetActive(false);
        if (hitboxAttack3 != null) hitboxAttack3.SetActive(false);
    }

    public void Initialize(Player player)
    {
        _player = player;
        if (BodyRenderer != null) _originalColor = BodyRenderer.material.color;
    }

    public void OnEnable()
    {
        if (_generalTrigger != null) _generalTrigger.OnHit += HandleHit;
        if (_trigger12 != null) _trigger12.OnHit += HandleHit;
        if (_trigger3 != null) _trigger3.OnHit += HandleHit;
    }

    public void OnDisable()
    {
        if (_generalTrigger != null) _generalTrigger.OnHit -= HandleHit;
        if (_trigger12 != null) _trigger12.OnHit -= HandleHit;
        if (_trigger3 != null) _trigger3.OnHit -= HandleHit;

        _hitEnemies.Clear();
        _hitBreakables.Clear();
    }

    private HitboxTrigger SetupHitbox(GameObject go)
    {
        if (go == null) return null;

        if (go.GetComponent<HitboxTrigger>() == null) go.AddComponent<HitboxTrigger>();

        Collider col = go.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        Rigidbody rb = go.GetComponent<Rigidbody>();
        if (rb == null) rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        foreach (Collider childCol in go.GetComponentsInChildren<Collider>(true))
        {
            childCol.isTrigger = true;
            HitboxProxy proxy = childCol.gameObject.GetComponent<HitboxProxy>();
            if (proxy == null) proxy = childCol.gameObject.AddComponent<HitboxProxy>();
            proxy.parent = go.GetComponent<HitboxTrigger>();
        }

        return go.GetComponent<HitboxTrigger>();
    }

    public void OnEquip()
    {
        _hitEnemies.Clear();
        _hitBreakables.Clear();
        ResetCombo();
    }

    public void OnUnequip()
    {
        DisableAllHitboxes();
    }

    public void OnAttackInput()
    {
        if (_wasHit)
        {
            ResetCombo();
            _wasHit = false;
        }

        Animator anim = Animator;
        if (anim != null)
        {
            anim.ResetTrigger(Attack1Hash);
            anim.ResetTrigger(Attack2Hash);
            anim.ResetTrigger(Attack3Hash);
            anim.ResetTrigger(HeavyAttackHash);
        }

        _lastAttackTime = Time.time;
        _comboStep++;
        _isComboWindowOpen = false;
        _fallbackTimer = 0;

        float currentDamage = weaponBaseDamage;
        float currentCritChance = baseCritChance;
        float currentKnockback = 3f;
        Color comboColor = Color.white;
        string targetState = "Attack_01";
        AudioClip clipToPlay = null;
        bool useRandomLightClip = false;

        switch (_comboStep)
        {
            case 1:
                comboColor = Color.white;
                targetState = "Attack_01";
                useRandomLightClip = true;
                break;
            case 2:
                currentDamage *= 1.2f;
                currentKnockback = 5f;
                comboColor = Color.yellow;
                targetState = "Attack_02";
                useRandomLightClip = true;
                break;
            case 3:
                currentDamage *= 1.5f;
                currentCritChance += 0.25f;
                currentKnockback = 8f;
                comboColor = Color.red;
                targetState = "Attack_03";
                clipToPlay = specialAttackClip;
                break;
            default:
                ResetCombo();
                _comboStep = 1;
                targetState = "Attack_01";
                useRandomLightClip = true;
                break;
        }

        if (useRandomLightClip) SoundManager.Instance.PlayRandomSound(lightAttackClips);
        else if (clipToPlay != null) SoundManager.Instance.PlaySound(clipToPlay);

        if (anim != null)
        {
            anim.CrossFadeInFixedTime(targetState, 0.05f);
            Debug.Log($"[COMBO] Playing {targetState} (Step {_comboStep})");
        }

        HitboxTrigger activeTrigger = (_comboStep == 3) ? _trigger3 : _trigger12;
        GameObject activeHitboxGO = (_comboStep == 3) ? hitboxAttack3 : hitboxAttack12;
        if (activeTrigger == null || activeHitboxGO == null)
        {
            activeTrigger = _generalTrigger;
            activeHitboxGO = generalAttackHitbox;
        }

        _hitEnemies.Clear();
        _hitBreakables.Clear();
        _currentDamage = currentDamage;
        _currentCritChance = currentCritChance;
        _currentKnockback = currentKnockback;

        if (activeHitboxGO != null) activeHitboxGO.SetActive(true);
        _hasActiveHitbox = activeHitboxGO != null;

        SetPlayerColor(comboColor);
    }

    public void OnHeavyAttackInput()
    {
        _specialTimer = specialCooldown;
        _lastAttackTime = Time.time;
        _fallbackTimer = 0;

        Transform pt = PlayerTransform;
        if (pt != null)
        {
            Vector3 pos = pt.position;
            pos.y = 0;
            pt.position = pos;
        }

        Renderer br = BodyRenderer;
        if (br != null) br.material.color = Color.cyan;

        Animator anim = Animator;
        if (anim != null)
        {
            anim.ResetTrigger(Attack1Hash);
            anim.ResetTrigger(Attack2Hash);
            anim.ResetTrigger(Attack3Hash);
            anim.ResetTrigger(HeavyAttackHash);
            anim.CrossFadeInFixedTime(HeavyAttackState, 0.05f);
        }
    }

    public void ExecuteHeavyDamage()
    {
        Debug.Log("Executing Special Attack (Sphere AOE)");

        Transform pt = PlayerTransform;
        Vector3 origin = pt != null ? pt.position : transform.position;
        LayerMask layer = EnemyLayer;
        Collider[] hitEnemies = Physics.OverlapSphere(origin, specialRange, layer);
        foreach (Collider enemy in hitEnemies)
        {
            Health h = enemy.GetComponentInParent<Health>();
            if (h != null) h.TakeDamage(weaponBaseDamage * 2, origin, specialKnockbackForce);
        }

        StartCoroutine(ShowSpecialAOEVisual());
    }

    private IEnumerator ShowSpecialAOEVisual()
    {
        showSpecialGizmo = true;
        yield return new WaitForSeconds(0.3f);
        showSpecialGizmo = false;
    }

    public void OnAnimationEvent(string evt)
    {
        switch (evt)
        {
            case "OpenComboWindow": _isComboWindowOpen = true; break;
            case "CloseComboWindow": _isComboWindowOpen = false; break;
            case "EnableHitbox": EnableHitboxForCombo(); break;
            case "DisableHitbox": DisableAllHitboxes(); break;
            case "ExecuteHeavyDamage": ExecuteHeavyDamage(); break;
            case "ReturnToIdle":
                if (Time.time - _lastAttackTime > 0.15f)
                    ResetCombo();
                break;
            case "ResetCombo":
                ResetCombo();
                break;
            case "DebugPlayAttack3":
                _comboStep = 3;
                Animator anim = Animator;
                if (anim != null) anim.SetTrigger(Attack3Hash);
                break;
        }
    }

    private void EnableHitboxForCombo()
    {
        if (_comboStep == 0) return;

        Debug.Log($"Hitbox ENABLED via Animation Event. Combo Step: {_comboStep}");

        if (_comboStep == 3) EnableHitbox3();
        else EnableHitbox12();
    }

    public void EnableHitbox12()
    {
        if (hitboxAttack3 != null) hitboxAttack3.SetActive(false);
        if (generalAttackHitbox != null) generalAttackHitbox.SetActive(false);

        if (hitboxAttack12 != null)
        {
            hitboxAttack12.SetActive(true);
            Debug.Log("Activated Hitbox_attack12 specifically");
        }
        else if (generalAttackHitbox != null) generalAttackHitbox.SetActive(true);
    }

    public void EnableHitbox3()
    {
        if (hitboxAttack12 != null) hitboxAttack12.SetActive(false);
        if (generalAttackHitbox != null) generalAttackHitbox.SetActive(false);

        if (hitboxAttack3 != null)
        {
            Debug.Log($"[HITBOX DEBUG] Attempting to activate Hitbox_attack3. Current State: {hitboxAttack3.activeSelf}");
            hitboxAttack3.SetActive(true);
            Debug.Log($"[HITBOX DEBUG] Hitbox_attack3 is now: {hitboxAttack3.activeInHierarchy}");
        }
        else
        {
            Debug.LogError("[HITBOX DEBUG] Hitbox_attack3 is NULL! Please check the Inspector.");
            if (generalAttackHitbox != null) generalAttackHitbox.SetActive(true);
        }
    }

    public void DisableAllHitboxes()
    {
        Debug.Log("Hitboxes DISABLED");
        if (generalAttackHitbox != null) generalAttackHitbox.SetActive(false);
        if (hitboxAttack12 != null) hitboxAttack12.SetActive(false);
        if (hitboxAttack3 != null) hitboxAttack3.SetActive(false);
        _hasActiveHitbox = false;
    }

    public void OnDamageReceived()
    {
        _wasHit = true;
        DisableAllHitboxes();
        ResetCombo();
    }

    public bool ShouldResetCombo()
    {
        return _comboStep > 0 && Time.time - _lastAttackTime > comboResetTime;
    }

    public void TickTimers()
    {
        if (_specialTimer > 0) _specialTimer -= Time.deltaTime;

        if (_comboStep > 0 && Time.time - _lastAttackTime > comboResetTime)
            ResetCombo();
    }

    public void TickFallback(float attackFailsafeSeconds)
    {
        if (_comboStep > 0 || _specialTimer == specialCooldown)
        {
            _fallbackTimer += Time.deltaTime;
            if (_fallbackTimer > attackFailsafeSeconds)
            {
                Debug.LogWarning($"[FAILSAFE] Stuck in attack for {_fallbackTimer:F1}s. Forcing reset.");
                ResetCombo();
            }
        }
        else
        {
            _fallbackTimer = 0f;
        }
    }

    public bool CanUseSpecial => _specialTimer <= 0f;

    public float GetDamage() => weaponBaseDamage;
    public float GetKnockBackForce() => knockBackForce;

    public void ResetCombo()
    {
        _comboStep = 0;
        _isComboWindowOpen = false;
        SetPlayerColor(_originalColor);
        Animator anim = Animator;
        if (anim != null) anim.ResetTrigger(HeavyAttackHash);
    }

    private void SetPlayerColor(Color color)
    {
        Renderer br = BodyRenderer;
        if (br != null) br.material.color = color;
    }

    private void HandleHit(Collider other)
    {
        Health targetHealth = other.GetComponentInParent<Health>();

        if (targetHealth != null)
        {
            bool isEnemy = other.CompareTag("Enemy")
                || (other.transform.root != null && other.transform.root.CompareTag("Enemy"))
                || other.gameObject.layer == 6;

            if (isEnemy && !_hitEnemies.Contains(targetHealth) && targetHealth.gameObject != transform.root.gameObject)
            {
                bool isCrit = Random.value < _currentCritChance;
                float finalDamage = isCrit ? _currentDamage * 2 : _currentDamage;

                targetHealth.TakeDamage(finalDamage, transform.root.position, _currentKnockback);
                _hitEnemies.Add(targetHealth);

                Debug.Log($"[Combat] {transform.root.name} hit {targetHealth.gameObject.name} for {finalDamage} damage");
            }
        }
        else if (other.CompareTag("Breakeable"))
        {
            Breakable_Objects breakable = other.GetComponent<Breakable_Objects>();
            if (breakable != null && !_hitBreakables.Contains(breakable))
            {
                breakable.TakeDamage(1);
                _hitBreakables.Add(breakable);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (showSpecialGizmo)
        {
            Gizmos.color = new Color(0, 1, 1, 0.4f);
            Transform pt = PlayerTransform;
            Vector3 origin = pt != null ? pt.position : transform.position;
            Gizmos.DrawSphere(origin, specialRange);
        }

        Vector3 forward = transform.forward;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + forward, attackRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position + forward, specialRange);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward, attackRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position + transform.forward, specialRange);
    }
}
