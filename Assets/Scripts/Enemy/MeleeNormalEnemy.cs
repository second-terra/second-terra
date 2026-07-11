using UnityEngine;

public class MeleeNormalEnemy : MeleeEnemyBase
{
    private enum EnemyState { Chase, Attack }

    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float attackExitMargin = 1.1f;

    private EnemyState state = EnemyState.Chase;
    private float lastAttackTime = -99f;

    private void Update()
    {
        if (isDead || !HasPlayer) return;

        switch (state)
        {
            case EnemyState.Chase:  UpdateChase();  break;
            case EnemyState.Attack: UpdateAttack(); break;
        }
    }

    private void UpdateChase()
    {
        if (ChaseOrTrigger(attackRange, moveSpeed))
            state = EnemyState.Attack;
    }

    private void UpdateAttack()
    {
        if (DistToPlayer() > attackRange * attackExitMargin)
        {
            state = EnemyState.Chase;
            return;
        }

        FaceTarget();

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            lastAttackTime = Time.time;
            PerformAttack(playerDamageable);
        }
    }

    private void FaceTarget()
    {
        Vector2 dir = DirToPlayer();
        if (dir == Vector2.zero) return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.Euler(0f, 0f, angle),
            Time.deltaTime * rotationSpeed);
    }

    protected override void PerformAttack(IDamageable target)
    {
        if (target == null) return;

        target.TakeDamage(attackDamage);
        Debug.Log($"[근거리 일반형] 공격! 데미지: {attackDamage}");
    }
}
