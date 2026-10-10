using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ScissorsWeaponGizmos))]
public class ScissorsWeapon : MonoBehaviour, IWeapon
{

#region Fields

    [Header("Configuration")]
    [SerializeField] private ScissorsWeaponData _data;

    [Header("Combo Steps (leave empty for defaults)")]
    [SerializeField] private ScissorsComboResult[] _comboSteps;

    [Header("Hitboxes (manual)")]
    [SerializeField] private GameObject generalAttackHitbox;
    [SerializeField] private GameObject hitboxAttack12;
    [SerializeField] private GameObject hitboxAttack3;

    [Header("Audio Clips (manual)")]
    [SerializeField] private List<AudioClip> lightAttackClips;
    [SerializeField] private AudioClip specialAttackClip;

    [Header("Debug")]
    [SerializeField] private bool showSpecialGizmo = false;

    // Hitbox indices in the rig array (order: general, h12, h3)
    private const int GeneralHitboxIndex = 0;
    private const int Hitbox12Index = 1;
    private const int Hitbox3Index = 2;

    // Defaults used when the Inspector _comboSteps array is empty.
    // Matches the original GetScissorsStepResult switch behavior.
    private static readonly ScissorsComboResult[] _defaultComboSteps = new[]
    {
        new ScissorsComboResult
        {
            DamageMultiplier = 1f, CritBonus = 0f, Knockback = 3f,
            Color = Color.white, AnimState = "Attack_01",
            HitboxIndex = Hitbox12Index, UseRandomLightClip = true, AudioClip = null,
        },
        new ScissorsComboResult
        {
            DamageMultiplier = 1.2f, CritBonus = 0f, Knockback = 5f,
            Color = Color.yellow, AnimState = "Attack_02",
            HitboxIndex = Hitbox12Index, UseRandomLightClip = true, AudioClip = null,
        },
        new ScissorsComboResult
        {
            DamageMultiplier = 1.5f, CritBonus = 0.25f, Knockback = 8f,
            Color = Color.red, AnimState = "Attack_03",
            HitboxIndex = Hitbox3Index, UseRandomLightClip = false, AudioClip = null,
        },
    };

    // Runtime state
    private Player _player;
    private WeaponComboSystem _combo;
    private WeaponHitboxRig _rig;
    private List<Health> _hitEnemies = new List<Health>();
    private List<Breakable_Objects> _hitBreakables = new List<Breakable_Objects>();
    private float _specialTimer = 0f;
    private float _lastHeavyTime = 0f;
    private bool _wasHit = false;
    private float _currentDamage;
    private float _currentCritChance;
    private float _currentKnockback;
    private Color _originalColor;

    private static readonly int HeavyAttackHash = Animator.StringToHash("HeavyAttack");
    private const string HeavyAttackState = "HeavyAttack";

#endregion
#region Unity Lifecycle

    private void Awake()
    {
        // Work on a runtime copy so shop upgrades don't mutate the shared asset.
        if (_data != null) _data = Instantiate(_data);
    }

    public void Initialize(Player player)
    {
        _player = player;
        if (BodyRenderer != null) _originalColor = BodyRenderer.material.color;

        _combo = new WeaponComboSystem(_data.comboResetTime, _data.attackCooldown, _defaultComboSteps.Length);
        _rig = new WeaponHitboxRig(
            new[] { generalAttackHitbox, hitboxAttack12, hitboxAttack3 },
            HandleHit);
        _rig.DisableAll();
        _rig.SubscribeAll();
    }

    // Re-subscribe the hitbox rig after a disable/enable cycle so hits land
    // again if the weapon is pooled or the GameObject is toggled.
    private void OnEnable()
    {
        if (_rig != null) _rig.SubscribeAll();
    }

    public void OnDisable()
    {
        _rig?.UnsubscribeAll();
        _hitEnemies.Clear();
        _hitBreakables.Clear();
    }

    private void OnDestroy()
    {
        // Destroy the runtime copy of the ScriptableObject asset to avoid leaking it
        // across Play mode sessions in the Editor.
        if (_data != null) Destroy(_data);
    }

    public void OnEquip()
    {
        _hitEnemies.Clear();
        _hitBreakables.Clear();
        ResetCombo();
    }

    public void OnUnequip()
    {
        _rig?.DisableAll();
    }

#endregion
#region Combat

    public void OnAttackInput()
    {
        if (_wasHit)
        {
            ResetCombo();
            _wasHit = false;
        }

        ComboStep step = _combo.AdvanceStep();
        ScissorsComboResult result = GetStep(step.Index);

        _currentDamage = _data.weaponBaseDamage * result.DamageMultiplier;
        _currentCritChance = Mathf.Clamp01(_data.baseCritChance + result.CritBonus);
        _currentKnockback = result.Knockback;

        PlayStepSound(result);

        Animator anim = Animator;
        if (anim != null)
        {
            anim.CrossFadeInFixedTime(result.AnimState, 0.05f);
            Debug.Log($"[COMBO] Playing {result.AnimState} (Step {step.Index})");
        }

        _hitEnemies.Clear();
        _hitBreakables.Clear();

        SetPlayerColor(result.Color);
    }

    public void OnHeavyAttackInput()
    {
        _specialTimer = _data.specialCooldown;
        _lastHeavyTime = Time.time;

        Renderer br = BodyRenderer;
        if (br != null) br.material.color = Color.cyan;

        Animator anim = Animator;
        if (anim != null)
        {
            anim.ResetTrigger(HeavyAttackHash);
            anim.SetTrigger(HeavyAttackHash);
            anim.CrossFadeInFixedTime(HeavyAttackState, 0.05f);
        }
    }

    public void ExecuteHeavyDamage()
    {
        Debug.Log("Executing Special Attack (Sphere AOE)");

        Transform pt = PlayerTransform;
        Vector3 origin = pt != null ? pt.position : transform.position;
        LayerMask layer = EnemyLayer;
        Collider[] hitEnemies = Physics.OverlapSphere(origin, _data.specialRange, layer);
        foreach (Collider enemy in hitEnemies)
        {
            Health h = enemy.GetComponentInParent<Health>();
            if (h != null) h.TakeDamage(_data.weaponBaseDamage * 2, origin, _data.specialKnockbackForce);
        }

        StartCoroutine(ShowSpecialAOEVisual());
    }

    private IEnumerator ShowSpecialAOEVisual()
    {
        showSpecialGizmo = true;
        yield return new WaitForSeconds(0.3f);
        showSpecialGizmo = false;
    }

#endregion
#region Combo Management

    private ScissorsComboResult GetStep(int index)
    {
        ScissorsComboResult[] source = (_comboSteps != null && _comboSteps.Length > 0)
            ? _comboSteps
            : _defaultComboSteps;
        int clamped = Mathf.Clamp(index - 1, 0, source.Length - 1);
        ScissorsComboResult step = source[clamped];

        // Step 3 (finisher) uses the special attack audio clip from the weapon instance.
        if (!step.UseRandomLightClip)
            step.AudioClip = specialAttackClip;

        return step;
    }

    private void PlayStepSound(ScissorsComboResult result)
    {
        if (SoundManager.Instance == null) return;

        if (result.UseRandomLightClip && lightAttackClips != null && lightAttackClips.Count > 0)
            SoundManager.Instance.PlayRandomSound(lightAttackClips);
        else if (result.AudioClip != null)
            SoundManager.Instance.PlaySound(result.AudioClip);
    }

    public void OnDamageReceived()
    {
        _wasHit = true;
        _rig?.DisableAll();
        ResetCombo();
    }

    public bool ShouldResetCombo()
    {
        return _combo != null && _combo.ShouldResetByTimeout();
    }

    public void TickTimers()
    {
        if (_specialTimer > 0) _specialTimer -= Time.deltaTime;
        if (_combo != null && _combo.ShouldResetByTimeout())
            ResetCombo();
    }

    public void ResetCombo()
    {
        _combo?.Reset();
        SetPlayerColor(_originalColor);
        Animator anim = Animator;
        if (anim != null) anim.ResetTrigger(HeavyAttackHash);
    }

#endregion
#region Animation Events

    public void OnAnimationEvent(string evt)
    {
        switch (evt)
        {
            case "OpenComboWindow":
                if (_combo != null) _combo.OpenWindow();
                break;
            case "CloseComboWindow":
                if (_combo != null) _combo.CloseWindow();
                break;
            case "EnableHitbox":
                EnableHitboxForCombo();
                break;
            case "EnableHitbox12":
                _rig.Enable(Hitbox12Index);
                break;
            case "EnableHitbox3":
                _rig.Enable(Hitbox3Index);
                break;
            case "DisableHitbox":
                _rig.DisableAll();
                break;
            case "ExecuteHeavyDamage":
                ExecuteHeavyDamage();
                break;
            case "ReturnToIdle":
                break;
            case "ResetCombo":
                ResetCombo();
                break;
            case "DebugPlayAttack3":
                while (_combo != null && _combo.CurrentStep < 3) _combo.AdvanceStep();
                Animator anim = Animator;
                if (anim != null) anim.CrossFadeInFixedTime("Attack_03", 0.05f);
                break;
        }
    }

    private void EnableHitboxForCombo()
    {
        if (_combo == null || _combo.CurrentStep == 0) return;

        Debug.Log($"Hitbox ENABLED via Animation Event. Combo Step: {_combo.CurrentStep}");

        int hitboxIndex = GetStep(_combo.CurrentStep).HitboxIndex;
        if (_rig.GetHitbox(hitboxIndex) == null) hitboxIndex = GeneralHitboxIndex;
        _rig.Enable(hitboxIndex);
    }

#endregion
#region Hit Handling

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

#endregion
#region Properties

    public ScissorsWeaponData Data => _data;

    public string Name => "Scissors";

    public float BaseDamage
    {
        get => _data != null ? _data.weaponBaseDamage : 0f;
        set { if (_data != null) _data.weaponBaseDamage = value; }
    }

    public float BaseCritChance
    {
        get => _data != null ? _data.baseCritChance : 0f;
        set { if (_data != null) _data.baseCritChance = value; }
    }

    public bool IsComboWindowOpen => _combo != null && _combo.IsWindowOpen;
    public float LastAttackTime => Mathf.Max(
        _combo != null ? _combo.LastAttackTime : 0f,
        _lastHeavyTime);
    public int CurrentComboStep => _combo != null ? _combo.CurrentStep : 0;
    public bool HasActiveHitbox => _rig != null && _rig.HasActiveHitbox;
    public bool CanAttack => _combo != null && _combo.CanAttack;
    public float AttackCooldownRemaining => _combo != null ? _combo.AttackCooldownRemaining : 0f;
    public float SpecialTimer => _specialTimer;
    public float AttackAnimationSpeed => _data != null ? _data.attackAnimationSpeed : 1.5f;
    public float SpecialAnimationSpeed => _data != null ? _data.specialAttackAnimationSpeed : 1.2f;
    public bool CanUseSpecial => _specialTimer <= 0f;

    public float GetDamage() => _data != null ? _data.weaponBaseDamage : 0f;
    public float GetKnockBackForce() => _data != null ? _data.knockBackForce : 0f;

    // Internal accessors for ScissorsWeaponGizmos
    internal bool ShowSpecialGizmo
    {
        get => showSpecialGizmo;
        set => showSpecialGizmo = value;
    }
    internal float AttackRange => _data != null ? _data.attackRange : 0f;
    internal float SpecialRange => _data != null ? _data.specialRange : 0f;
    internal WeaponHitboxRig Rig => _rig;

    // Player-facade shortcuts
    private Animator Animator => _player != null ? _player.animator : null;
    private Renderer BodyRenderer => _player != null ? _player.bodyRenderer : null;
    private LayerMask EnemyLayer => _player != null ? _player.enemyLayer : 0;
    internal Transform PlayerTransform => _player != null ? _player.transform : transform;

#endregion

}
