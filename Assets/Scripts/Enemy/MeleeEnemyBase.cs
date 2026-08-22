using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class MeleeEnemyBase : EnemyBase
{
    protected Rigidbody2D rb;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
    }

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
