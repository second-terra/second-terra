using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class RangedEnemyBase : EnemyBase
{
    [Header("조준")]
    [SerializeField] private float rotationSpeed = 8f;

    [Header("거리 유지(카이팅)")]
    [SerializeField] private float preferredRange = 6f;
    [SerializeField] private float retreatRange = 3f;
    [SerializeField] private float kitingMoveSpeed = 2.5f;

    protected Rigidbody2D rb;

    private float lastAttackTime = -99f;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
    }

    protected virtual void Update()
    {
        if (isDead || !HasPlayer) { StopMoving(); return; }
        if (DistToPlayer() > detectionRange) { StopMoving(); return; }

        UpdateKiting();
        FaceTarget();

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            lastAttackTime = Time.time;
            PerformAttack(playerDamageable);
        }
    }

    // 너무 멀면 접근하고, 너무 가까우면 뒤로 빠지고, 그 사이(선호 거리)면 멈춰서 공격 위치를 유지한다.
    protected void UpdateKiting()
    {
        float dist = DistToPlayer();

        if (dist < retreatRange)
            Move(-DirToPlayer(), kitingMoveSpeed);
        else if (dist > preferredRange)
            Move(DirToPlayer(), kitingMoveSpeed);
        else
            StopMoving();
    }

    protected void Move(Vector2 direction, float speed)
    {
        rb.velocity = direction * speed;
    }

    protected void StopMoving()
    {
        rb.velocity = Vector2.zero;
    }

    protected void FaceTarget()
    {
        Vector2 dir = DirToPlayer();
        if (dir == Vector2.zero) return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.Euler(0f, 0f, angle),
            Time.deltaTime * rotationSpeed);
    }

    protected override void OnDied()
    {
        StopMoving();
        base.OnDied();
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, preferredRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, retreatRange);
    }
}
