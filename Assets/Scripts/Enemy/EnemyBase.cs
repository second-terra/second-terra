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

    private float baseMoveSpeed;
    private float baseAttackCooldown;

    private class SlowEffect
    {
        public float MoveMultiplier;
        public float AttackSpeedMultiplier;
    }

    private readonly List<SlowEffect> activeSlows = new();

    // moveSpeed/attackCooldown을 직접 읽지 않는 하위 클래스(원거리 카이팅 속도, 보스 이동/템포 등)가
    // 자기 값에 곱해서 쓸 수 있도록 현재 둔화 배율을 노출한다. 겹친 둔화 중 가장 강한 것이 적용됨.
    public float SlowMoveMultiplier { get; private set; } = 1f;
    public float SlowAttackSpeedMultiplier { get; private set; } = 1f;

    protected Transform playerTransform;
    protected IDamageable playerDamageable;

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
        baseMoveSpeed = moveSpeed;
        baseAttackCooldown = attackCooldown;

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

        // 현재 이 프로젝트에서 적은 SetActive(false)로 재사용되지 않고 Destroy로 정리되지만,
        // 나중에 풀링 등으로 바뀌어도 비활성화 시 둔화 상태가 새지 않도록 방어적으로 정리.
        activeSlows.Clear();
        RecalculateSlow();
    }

    protected virtual void Start()
    {
        var healthBar = GetComponentInChildren<EnemyHealthBar>();
        if (healthBar != null)
            OnHealthChanged += healthBar.UpdateBar;

        var player = FindObjectOfType<PlayerStats>();
        if (player != null)
        {
            playerTransform = player.transform;
            playerDamageable = player;
        }
        else
        {
            Debug.LogWarning($"[{GetType().Name}] 씬에서 PlayerStats를 찾지 못했습니다.");
        }
    }

    protected bool HasPlayer => playerTransform != null;

    protected float DistToPlayer() =>
        Vector2.Distance(transform.position, playerTransform.position);

    protected Vector2 DirToPlayer() =>
        ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        amount = Mathf.Max(0f, amount);
        amount = ModifyIncomingDamage(amount);

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

    // 이동속도/공격속도를 일정 시간 둔화시킨다 (예: 드론 음파 스킬). 지속시간이 지나면 자동 복구.
    // 여러 둔화가 겹치면 그중 가장 강한(배율이 가장 작은) 효과가 적용되고, 하나가 만료돼도
    // 아직 남은 둔화 중 가장 강한 것으로 다시 계산된다 (약한 둔화가 강한 둔화를 덮어쓰지 않음).
    public void ApplySlow(float moveSpeedMultiplier, float attackSpeedMultiplier, float duration)
    {
        if (isDead) return;
        if (!IsValidSlowArgs(moveSpeedMultiplier, attackSpeedMultiplier, duration)) return;

        var effect = new SlowEffect
        {
            MoveMultiplier = moveSpeedMultiplier,
            AttackSpeedMultiplier = attackSpeedMultiplier,
        };
        activeSlows.Add(effect);
        RecalculateSlow();

        StartCoroutine(RevertSlowAfter(effect, duration));
    }

    private static bool IsValidSlowArgs(float moveMultiplier, float attackSpeedMultiplier, float duration)
    {
        if (float.IsNaN(moveMultiplier) || float.IsInfinity(moveMultiplier) || moveMultiplier < 0f || moveMultiplier > 1f)
            return false;
        if (float.IsNaN(attackSpeedMultiplier) || float.IsInfinity(attackSpeedMultiplier) || attackSpeedMultiplier <= 0f || attackSpeedMultiplier > 1f)
            return false;
        if (float.IsNaN(duration) || float.IsInfinity(duration) || duration < 0f)
            return false;
        return true;
    }

    private IEnumerator RevertSlowAfter(SlowEffect effect, float duration)
    {
        yield return new WaitForSeconds(duration);

        activeSlows.Remove(effect);
        RecalculateSlow();
    }

    private void RecalculateSlow()
    {
        if (activeSlows.Count == 0)
        {
            moveSpeed = baseMoveSpeed;
            attackCooldown = baseAttackCooldown;
            SlowMoveMultiplier = 1f;
            SlowAttackSpeedMultiplier = 1f;
            return;
        }

        float moveMul = 1f;
        float atkMul = 1f;
        foreach (var s in activeSlows)
        {
            moveMul = Mathf.Min(moveMul, s.MoveMultiplier);
            atkMul = Mathf.Min(atkMul, s.AttackSpeedMultiplier);
        }

        moveSpeed = baseMoveSpeed * moveMul;
        attackCooldown = baseAttackCooldown / atkMul;
        SlowMoveMultiplier = moveMul;
        SlowAttackSpeedMultiplier = atkMul;
    }

    // 피격 플래시가 복귀할 "기본색"을 하위 클래스가 갱신할 수 있게 함 (예: 갑피 단계별 색 표시).
    protected void SetBaseTint(Color color)
    {
        if (spriteRenderer == null) return;
        originalColor = color;
        if (flashCoroutine == null)
            spriteRenderer.color = color;
    }

    // currentHp를 하위 클래스가 TakeDamage 경로 밖에서 직접 조정했을 때(예: 보스 부활) UI에 반영시키기 위함.
    protected void RaiseHealthChanged() => OnHealthChanged?.Invoke(currentHp, maxHp);

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

    protected virtual float ModifyIncomingDamage(float amount) => amount;
}
