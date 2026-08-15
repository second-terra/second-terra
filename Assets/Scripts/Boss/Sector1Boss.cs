using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sector1Boss : BossBase
{
    [Header("기믹 - 템포")]
    [SerializeField] private float minTempoScale = 0.4f;

    [Header("공격")]
    [SerializeField] private float meleeRange = 2f;
    [SerializeField] private float meleeDamage = 15f;
    [SerializeField] private float stillnessThreshold = 0.6f;
    [SerializeField] private float attackWindup = 0.3f;

    [Header("도약")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private int jumpRepeatCount = 5;
    [SerializeField] private float jumpPrepareTime = 0.5f;
    [SerializeField] private float jumpDistance = 5f;
    [SerializeField] private float jumpDuration = 0.3f;
    [SerializeField] private int jumpBurstCount = 8;
    [SerializeField] private float jumpProjectileSpeed = 6f;
    [SerializeField] private float jumpProjectileDamage = 10f;
    [SerializeField] private float jumpCycleGap = 0.4f;

    [Header("추적돌격")]
    [SerializeField] private int chargeRepeatCount = 3;
    [SerializeField] private float chargeWindup = 0.4f;
    [SerializeField] private float chargeDuration = 1.2f;
    [SerializeField] private float chargeSpeed = 9f;
    [SerializeField] private float chargeHitRange = 1f;
    [SerializeField] private float chargeDamage = 12f;
    [SerializeField] private float chargeHitCooldown = 0.3f;
    [SerializeField] private float chargeCycleGap = 0.3f;

    private PlayerController playerController;
    private Vector2 lastPlayerMoveDir = Vector2.down;
    private float stillnessTimer;
    private bool isContactDamaging;
    private float lastChargeHitTime = -99f;

    protected override void Start()
    {
        base.Start();
        if (playerTransform != null)
            playerController = playerTransform.GetComponent<PlayerController>();
    }

    protected override void Update()
    {
        base.Update();
        TrackPlayerMovement();

        if (isContactDamaging)
            CheckChargeContact();
    }

    private void TrackPlayerMovement()
    {
        if (playerController == null) return;

        Vector2 dir = playerController.MoveDirection;
        if (dir != Vector2.zero)
        {
            lastPlayerMoveDir = dir;
            stillnessTimer = 0f;
        }
        else
        {
            stillnessTimer += Time.deltaTime;
        }
    }

    // 추적돌격 중에만 접촉 데미지가 들어간다 — 그 외 패턴/대기 중에는 몸에 닿아도 무피해.
    private void CheckChargeContact()
    {
        if (!HasPlayer) return;
        if (Time.time - lastChargeHitTime < chargeHitCooldown) return;

        if (DistToPlayer() <= chargeHitRange)
        {
            lastChargeHitTime = Time.time;
            playerDamageable?.TakeDamage(chargeDamage);
        }
    }

    protected override float GetIntervalScale() =>
        Mathf.Lerp(minTempoScale, 1f, Mathf.Clamp01(currentHp / maxHp));

    protected override List<BossPattern> BuildPatterns()
    {
        return new List<BossPattern>
        {
            new BossPattern { Name = "공격", Weight = 1f, CanUse = CanUseAttack, Execute = Pattern_Attack },
            new BossPattern { Name = "도약", Weight = 1f, Execute = Pattern_Jump },
            new BossPattern { Name = "추적돌격", Weight = 1f, Execute = Pattern_Charge },
        };
    }

    private bool CanUseAttack() =>
        HasPlayer && DistToPlayer() <= meleeRange && stillnessTimer >= stillnessThreshold;

    private IEnumerator Pattern_Attack()
    {
        yield return new WaitForSeconds(attackWindup * TempoScale);

        if (HasPlayer && DistToPlayer() <= meleeRange * 1.3f)
            playerDamageable?.TakeDamage(meleeDamage);
    }

    private IEnumerator Pattern_Jump()
    {
        for (int i = 0; i < jumpRepeatCount; i++)
        {
            if (!HasPlayer) yield break;

            yield return new WaitForSeconds(jumpPrepareTime * TempoScale);

            Vector2 dir = lastPlayerMoveDir;
            Vector2 startPos = rb.position;

            float duration = Mathf.Max(0.05f, jumpDuration * TempoScale);
            rb.velocity = dir * (jumpDistance / duration);

            yield return new WaitForSeconds(duration);

            rb.velocity = Vector2.zero;
            Vector2 endPos = rb.position;

            EnemyProjectile.FireRadial(projectilePrefab, startPos, Vector2.right, jumpBurstCount, jumpProjectileSpeed, jumpProjectileDamage);
            EnemyProjectile.FireRadial(projectilePrefab, endPos, Vector2.right, jumpBurstCount, jumpProjectileSpeed, jumpProjectileDamage);

            yield return new WaitForSeconds(jumpCycleGap * TempoScale);
        }
    }

    private IEnumerator Pattern_Charge()
    {
        for (int i = 0; i < chargeRepeatCount; i++)
        {
            if (!HasPlayer) yield break;

            yield return new WaitForSeconds(chargeWindup * TempoScale);

            isContactDamaging = true;
            float elapsed = 0f;
            float duration = Mathf.Max(0.05f, chargeDuration * TempoScale);

            while (elapsed < duration && HasPlayer)
            {
                elapsed += Time.deltaTime;
                rb.velocity = DirToPlayer() * chargeSpeed;
                yield return null;
            }

            rb.velocity = Vector2.zero;
            isContactDamaging = false;

            yield return new WaitForSeconds(chargeCycleGap * TempoScale);
        }
    }

    protected override void OnDied()
    {
        rb.velocity = Vector2.zero;
        isContactDamaging = false;
        base.OnDied();
    }
}
