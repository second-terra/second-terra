using UnityEngine;

public abstract class RangedEnemyBase : EnemyBase
{
    [Header("조준")]
    [SerializeField] private float rotationSpeed = 8f;

    private float lastAttackTime = -99f;

    protected virtual void Update()
    {
        if (isDead || !HasPlayer) return;
        if (DistToPlayer() > detectionRange) return;

        FaceTarget();

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            lastAttackTime = Time.time;
            PerformAttack(playerDamageable);
        }
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

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
