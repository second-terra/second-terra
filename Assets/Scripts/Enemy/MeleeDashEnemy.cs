using UnityEngine;

public class MeleeDashEnemy : MeleeEnemyBase
{
    private enum EnemyState { Chase, Prepare, Dash, Recover }

    [Header("돌진")]
    [SerializeField] private float dashTriggerRange = 6f;
    [SerializeField] private float dashPrepareTime = 0.6f;
    [SerializeField] private float dashStartSpeed = 6f;
    [SerializeField] private float dashMaxSpeed = 14f;
    [SerializeField] private float dashAccelTime = 0.25f;
    [SerializeField] private float dashDuration = 0.5f;
    [SerializeField] private float dashHitRange = 0.8f;
    [SerializeField] private float recoverTime = 0.8f;
    [SerializeField] private float dashDamageMultiplier = 2f;

    private EnemyState state = EnemyState.Chase;
    private float stateTimer;
    private float dashElapsed;
    private Vector2 dashDirection;

    private void Update()
    {
        if (isDead || !HasPlayer) return;

        switch (state)
        {
            case EnemyState.Chase:   UpdateChase();   break;
            case EnemyState.Prepare: UpdatePrepare(); break;
            case EnemyState.Dash:    UpdateDash();    break;
            case EnemyState.Recover: UpdateRecover(); break;
        }
    }

    private void UpdateChase()
    {
        if (ChaseOrTrigger(dashTriggerRange, moveSpeed))
            EnterPrepare();
    }

    private void EnterPrepare()
    {
        stateTimer = dashPrepareTime;
        state = EnemyState.Prepare;
    }

    private void UpdatePrepare()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
            EnterDash();
    }

    private void EnterDash()
    {
        dashDirection = DirToPlayer();
        stateTimer = dashDuration;
        dashElapsed = 0f;
        state = EnemyState.Dash;
    }

    private void UpdateDash()
    {
        float speed = Mathf.Lerp(dashStartSpeed, dashMaxSpeed, dashAccelTime <= 0f ? 1f : dashElapsed / dashAccelTime);
        Move(dashDirection, speed);
        dashElapsed += Time.deltaTime;

        if (HasPlayer && DistToPlayer() <= dashHitRange)
        {
            PerformAttack(playerDamageable);
            EnterRecover();
            return;
        }

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
            EnterRecover();
    }

    private void EnterRecover()
    {
        StopMoving();
        stateTimer = recoverTime;
        state = EnemyState.Recover;
    }

    private void UpdateRecover()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
            state = EnemyState.Chase;
    }

    protected override void PerformAttack(IDamageable target)
    {
        if (target == null) return;

        float damage = EnemyBalance.NormalMeleeDamage * dashDamageMultiplier;
        target.TakeDamage(damage);
        Debug.Log($"[돌진형] 돌진 명중! 데미지: {damage}");
    }

    protected override void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, dashTriggerRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, dashHitRange);
    }
}
