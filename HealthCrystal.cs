using System;
using UnityEngine;
using PixelCrushers;

/// <summary>
/// Runtime Health Crystal for boss recharge/regeneration.
///
/// Responsibilities:
/// - Manages its own health pool.
/// - Implements IArrowTarget so arrow code can call OnArrowHit.
/// - Emits signals to Quest Machine via MessageSystem:
///     "CrystalDamaged"     — every hit (value = normalized health)
///     "CrystalThreatened"  — once, when below threatenedThreshold
///     "CrystalDestroyed"   — once, on death
/// - Feeds a defending dragon health while it is in range.
///
/// The crystal does NOT decide when to feed. The dragon's defend executor
/// (on the dragon) calls BeginFeeding / StopFeeding in response to the
/// brain's "Defend" / "CrystalDestroyed" / "StopDefend" messages.
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class HealthCrystal : MonoBehaviour, IArrowTarget
{
    [Tooltip("Human friendly name shown in debug lists and logs.")]
    public string displayName = "HealthCrystal";

    [Tooltip("Maximum hit points of the crystal.")]
    public float maxHealth = 200f;

    [Tooltip("When true the crystal will be destroyed once health hits zero.")]
    public bool destroyOnZero = true;

    [Header("Brain Signals")]
    [Range(0f, 1f)]
    [Tooltip("Crystal health fraction at which CrystalThreatened is sent.")]
    [SerializeField] private float threatenedThreshold = 0.5f;

    [Header("Dragon Feeding")]
    [Tooltip("Radius within which a defending dragon receives health from this crystal.")]
    [SerializeField] private float feedRadius = 15f;

    [Tooltip("Health per second transferred to a defending dragon.")]
    [SerializeField] private float feedRatePerSecond = 20f;

    private float currentHealth;
    private bool threatenedFired;
    private BossStatsAndHealth feedingDragon;

    public bool IsDestroyed => currentHealth <= 0f;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    public static event Action<HealthCrystal> OnCrystalSpawned;
    public static event Action<HealthCrystal> OnCrystalDestroyed;
    public static event Action<HealthCrystal> OnCrystalUnderAttack;

    private void Reset()
    {
        displayName = string.IsNullOrEmpty(displayName) ? gameObject.name : displayName;

        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            SphereCollider sc = gameObject.AddComponent<SphereCollider>();
            sc.isTrigger = false;
            sc.radius = 1f;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void Awake()
    {
        currentHealth = maxHealth;
        if (string.IsNullOrEmpty(displayName)) displayName = gameObject.name;

        OnCrystalSpawned?.Invoke(this);

        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            col = gameObject.AddComponent<SphereCollider>();
            col.isTrigger = false;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void Update()
    {
        if (IsDestroyed || feedingDragon == null) return;
        if (Vector3.Distance(transform.position, feedingDragon.transform.position) > feedRadius) return;

        feedingDragon.RestoreHealth(feedRatePerSecond * Time.deltaTime);
    }

    public void TakeDamage(float amount)
    {
        if (IsDestroyed) return;

        float dmg = Mathf.Abs(amount);
        currentHealth -= dmg;

        OnCrystalUnderAttack?.Invoke(this);

        MessageSystem.SendMessage(this, "CrystalDamaged", string.Empty, GetHealthNormalized());

        if (!threatenedFired && GetHealthNormalized() <= threatenedThreshold)
        {
            threatenedFired = true;
            MessageSystem.SendMessage(this, "CrystalThreatened", string.Empty, true);
        }

        Debug.Log($"[HealthCrystal] {displayName} took {dmg:F1} damage. Remaining: {currentHealth:F1}/{maxHealth:F1}");

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            Debug.Log($"[HealthCrystal] {displayName} destroyed!");
            OnCrystalDestroyed?.Invoke(this);

            MessageSystem.SendMessage(this, "CrystalDestroyed", string.Empty, true);

            if (destroyOnZero)
            {
                Destroy(gameObject, 0.1f);
            }
        }
    }

    public float GetHealthNormalized()
    {
        return maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
    }

    public void OnArrowHit(float damage, Vector3 impactPoint, ElementTypeOB7 elementType)
    {
        TakeDamage(damage);
    }

    /// <summary>Called by the dragon's defend executor when it reaches the crystal.</summary>
    public void BeginFeeding(BossStatsAndHealth dragon)
    {
        feedingDragon = dragon;
    }

    /// <summary>Called by the dragon's defend executor when it leaves the crystal.</summary>
    public void StopFeeding()
    {
        feedingDragon = null;
    }

    private void OnCollisionEnter(Collision collision)
    {
        var sticking = collision.collider.GetComponentInParent<StickingArrow>();
        if (sticking != null)
        {
            return;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.6f);
    }
}