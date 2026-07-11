using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class MeleeEnemyBase : EnemyBase
{
    protected Rigidbody2D rb;
    protected Transform playerTransform;
    protected IDamageable playerDamageable;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
    }

    protected override void Start()
    {
        base.Start();

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

    protected void Move(Vector2 direction, float speed)
    {
        rb.velocity = direction * speed;
    }

    protected void StopMoving()
    {
        rb.velocity = Vector2.zero;
    }

    protected bool ChaseOrTrigger(float triggerRange, float speed)
    {
        if (DistToPlayer() <= triggerRange)
        {
            StopMoving();
            return true;
        }

        Move(DirToPlayer(), speed);
        return false;
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
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
