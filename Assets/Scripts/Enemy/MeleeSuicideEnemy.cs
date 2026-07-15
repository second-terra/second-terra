using UnityEngine;

public class MeleeSuicideEnemy : MeleeEnemyBase
{
    private enum EnemyState { Chase, Delay }

    [Header("자폭")]
    [SerializeField] private float speedMultiplier = 1.4f;
    [SerializeField] private float contactRange = 0.8f;
    [SerializeField] private float explosionRadius = 1.5f;
    [SerializeField] private float fuseDelay = 0.6f;

    [SerializeField] private float selfDetonateDamageMultiplier = 2.5f;
    [SerializeField] private float deathExplodeDamageMultiplier = 0.5f;
    [SerializeField] private float splashDamageToOtherEnemies = EnemyBalance.NormalMeleeDamage;
    [SerializeField, Range(0f, 1f)] private float explosionChance = 0.5f;

    private EnemyState state = EnemyState.Chase;
    private float fuseTimer;
    private bool selfDetonating;

    private void Update()
    {
        if (isDead || !HasPlayer) return;

        switch (state)
        {
            case EnemyState.Chase: UpdateChase(); break;
            case EnemyState.Delay: UpdateDelay(); break;
        }
    }

    private void UpdateChase()
    {
        if (ChaseOrTrigger(contactRange, moveSpeed * speedMultiplier))
            EnterDelay();
    }

    private void EnterDelay()
    {
        fuseTimer = fuseDelay;
        state = EnemyState.Delay;
    }

    private void UpdateDelay()
    {
        fuseTimer -= Time.deltaTime;
        if (fuseTimer <= 0f)
            SelfDetonate();
    }

    private void SelfDetonate()
    {
        selfDetonating = true;
        Explode(EnemyBalance.NormalMeleeDamage * selfDetonateDamageMultiplier);
        Die();
    }

    private void Explode(float playerDamage)
    {
        if (Random.value > explosionChance)
        {
            Debug.Log("[자폭형] 불발...");
            return;
        }

        if (playerDamageable != null && HasPlayer && DistToPlayer() <= explosionRadius)
            playerDamageable.TakeDamage(playerDamage);

        foreach (var enemy in ActiveEnemies)
        {
            if (enemy == this || enemy.IsDead) continue;

            float dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist <= explosionRadius)
                enemy.TakeDamage(splashDamageToOtherEnemies);
        }

        Debug.Log($"[자폭형] 폭발! 플레이어 피해: {playerDamage}");
    }

    protected override void PerformAttack(IDamageable target) { }

    protected override void OnDied()
    {
        if (!selfDetonating)
            Explode(EnemyBalance.NormalMeleeDamage * deathExplodeDamageMultiplier);

        base.OnDied();
    }
}
