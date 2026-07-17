using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public abstract class EnemyBase : MonoBehaviour, IDamageable
{
    [Header("기본 스탯")]
    [SerializeField] protected float maxHp = 100f;
    [SerializeField] protected float attackDamage = EnemyBalance.NormalMeleeDamage;
    [SerializeField] protected float moveSpeed = 3f;
    [SerializeField] protected float attackRange = 1.5f;
    [SerializeField] protected float detectionRange = 8f;
    [SerializeField] protected float attackCooldown = 1.5f;

    [Header("피격 연출")]
    [SerializeField] private float hitFlashDuration = 0.1f;

    protected float currentHp;
    protected bool isDead;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine flashCoroutine;

    public event Action<float, float> OnHealthChanged;
    public event Action OnDeath;

    public bool IsDead => isDead;
    public float CurrentHp => currentHp;
    public float MaxHp => maxHp;

    private static readonly List<EnemyBase> activeEnemies = new();
    public static IReadOnlyList<EnemyBase> ActiveEnemies => activeEnemies;

    protected virtual void Awake()
    {
        currentHp = maxHp;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    protected virtual void OnEnable()
    {
        activeEnemies.Add(this);
    }

    protected virtual void OnDisable()
    {
        activeEnemies.Remove(this);
    }

    protected virtual void Start()
    {
        var healthBar = GetComponentInChildren<EnemyHealthBar>();
        if (healthBar != null)
            OnHealthChanged += healthBar.UpdateBar;
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        amount = Mathf.Max(0f, amount);

        currentHp = Mathf.Max(0f, currentHp - amount);
        OnHealthChanged?.Invoke(currentHp, maxHp);
        OnDamaged(amount);

        if (currentHp <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        amount = Mathf.Max(0f, amount);

        currentHp = Mathf.Min(maxHp, currentHp + amount);
        OnHealthChanged?.Invoke(currentHp, maxHp);
    }

    protected virtual void OnDamaged(float damage)
    {
        if (spriteRenderer == null) return;

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashWhite());
    }

    private IEnumerator FlashWhite()
    {
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(hitFlashDuration);
        spriteRenderer.color = originalColor;
        flashCoroutine = null;
    }

    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;
        OnDeath?.Invoke();
        OnDied();
    }

    protected virtual void OnDied()
    {
        Destroy(gameObject, 1.5f);
    }

    protected abstract void PerformAttack(IDamageable target);
}
