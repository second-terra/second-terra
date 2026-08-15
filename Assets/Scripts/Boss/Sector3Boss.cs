using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sector3Boss : BossBase
{
    [Header("기믹 - 부활")]
    [SerializeField] private float reviveHpFraction = 0.3f;
    [SerializeField] private float phase2DamageMultiplier = 1.3f;
    [SerializeField] private float phase2IntervalScale = 0.6f;

    [Header("소환")]
    [SerializeField] private GameObject summonedAddsPrefab;
    [SerializeField] private int summonCount = 4;
    [SerializeField] private float summonSpawnRadius = 2f;
    [SerializeField] private float summonSelfDamageFraction = 0.05f;

    [Header("도주")]
    [SerializeField] private float fleeLiquefyDuration = 1.2f;
    [SerializeField] private Color liquefyColor = new Color(1f, 1f, 1f, 0.3f);

    [Header("분출 - 곡사")]
    [SerializeField] private float arcZoneRadius = 2f;
    [SerializeField] private float arcTelegraphDuration = 0.8f;
    [SerializeField] private float arcActiveDuration = 5f;
    [SerializeField] private float arcTickInterval = 0.5f;
    [SerializeField] private float arcDamagePerTick = 6f;
    [SerializeField, Range(0f, 1f)] private float arcRetryChance = 0.5f;

    [Header("분출 - 직사")]
    [SerializeField] private GameObject trailProjectilePrefab;
    [SerializeField] private int lineSprayCount = 3;
    [SerializeField] private float lineSprayInterval = 0.15f;
    [SerializeField] private float lineSprayDamage = 10f;
    [SerializeField] private float lineSprayProjectileSpeed = 18f;

    private bool hasRevived;
    private bool isPhase2;
    private bool isInvulnerable;
    private Color normalColor;
    private float arcZoneExpireTime = -99f;

    protected override void Awake()
    {
        base.Awake();
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) normalColor = sr.color;
    }

    protected override float GetIntervalScale() => isPhase2 ? phase2IntervalScale : 1f;

    protected override float ModifyIncomingDamage(float amount) => isInvulnerable ? 0f : amount;

    // 스펙상 1회 한정 부활: 최대 체력의 reviveHpFraction으로 되살아나며 2페이즈로 전환.
    // 2페이즈 이후 재차 체력이 0이 되면 정상적으로 사망 처리.
    protected override void Die()
    {
        if (!hasRevived)
        {
            hasRevived = true;
            isPhase2 = true;
            currentHp = maxHp * reviveHpFraction;
            RaiseHealthChanged();
            return;
        }

        base.Die();
    }

    private float ApplyPhase2Damage(float baseDamage) => isPhase2 ? baseDamage * phase2DamageMultiplier : baseDamage;

    protected override List<BossPattern> BuildPatterns()
    {
        return new List<BossPattern>
        {
            new BossPattern { Name = "소환", Weight = 1f, Execute = Pattern_Summon },
            new BossPattern { Name = "도주", Weight = 1f, Execute = Pattern_Flee },
            new BossPattern { Name = "분출-곡사", Weight = 1f, CanUse = CanUseArcSpray, Execute = Pattern_ArcSpray },
            new BossPattern { Name = "분출-직사", Weight = 1f, Execute = Pattern_LineSpray },
        };
    }

    private bool CanUseArcSpray()
    {
        if (Time.time < arcZoneExpireTime) return false;
        return Random.value <= arcRetryChance;
    }

    private IEnumerator Pattern_Summon()
    {
        // 2페이즈에는 소환 시 스스로의 체력이 소모되지 않는다.
        if (!isPhase2)
        {
            float selfDamage = maxHp * summonSelfDamageFraction;
            currentHp = Mathf.Max(1f, currentHp - selfDamage);
            RaiseHealthChanged();
        }

        if (summonedAddsPrefab != null)
        {
            for (int i = 0; i < summonCount; i++)
            {
                Vector2 pos = (Vector2)transform.position + Random.insideUnitCircle * summonSpawnRadius;
                Instantiate(summonedAddsPrefab, pos, Quaternion.identity);
            }
        }

        yield return null;
    }

    private IEnumerator Pattern_Flee()
    {
        isInvulnerable = true;
        rb.velocity = Vector2.zero;
        SetBaseTint(liquefyColor);

        yield return new WaitForSeconds(fleeLiquefyDuration);

        isInvulnerable = false;
        SetBaseTint(normalColor);
    }

    private IEnumerator Pattern_ArcSpray()
    {
        Vector2 pos = HasPlayer ? (Vector2)playerTransform.position : (Vector2)transform.position;

        var zoneGo = new GameObject("ArcDamageZone");
        zoneGo.transform.position = pos;
        zoneGo.AddComponent<DamageZone>().Init(
            arcZoneRadius, arcTelegraphDuration, ApplyPhase2Damage(arcDamagePerTick),
            arcActiveDuration, arcTickInterval);

        arcZoneExpireTime = Time.time + arcTelegraphDuration + arcActiveDuration;

        yield return new WaitForSeconds(arcTelegraphDuration);
    }

    private IEnumerator Pattern_LineSpray()
    {
        if (trailProjectilePrefab == null) yield break;

        for (int i = 0; i < lineSprayCount; i++)
        {
            if (!HasPlayer) yield break;

            Vector2 dir = DirToPlayer();
            GameObject obj = Instantiate(trailProjectilePrefab, transform.position, Quaternion.identity);
            obj.GetComponent<TrailProjectile>().Init(dir, lineSprayProjectileSpeed, ApplyPhase2Damage(lineSprayDamage));

            yield return new WaitForSeconds(lineSprayInterval);
        }
    }

    protected override void OnDied()
    {
        rb.velocity = Vector2.zero;
        base.OnDied();
    }
}
