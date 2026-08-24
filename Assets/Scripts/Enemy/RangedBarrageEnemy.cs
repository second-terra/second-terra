using UnityEngine;

public class RangedBarrageEnemy : RangedEnemyBase
{
    public enum AimMode { Random, PlayerMoveDirection }
    public enum FirePattern { Radial, Straight }

    [Header("탄막")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private AimMode aimMode = AimMode.Random;
    [SerializeField] private FirePattern firePattern = FirePattern.Straight;
    [SerializeField] private int radialBulletCount = 8;
    [SerializeField] private float projectileSpeedFallback = 3f;

    private PlayerController playerController;

    protected override void Start()
    {
        base.Start();

        if (playerTransform != null)
            playerController = playerTransform.GetComponent<PlayerController>();
    }

    protected override void PerformAttack(IDamageable target)
    {
        if (target == null || projectilePrefab == null) return;

        float speed = playerController != null
            ? playerController.MoveSpeed * playerController.SpeedMultiplier * 0.6f
            : projectileSpeedFallback;

        Vector2 baseDir = GetAimDirection();

        foreach (var dir in GetFireDirections(baseDir))
            SpawnProjectile(dir, speed);
    }

    private Vector2 GetAimDirection()
    {
        switch (aimMode)
        {
            case AimMode.PlayerMoveDirection:
                Vector2 moveDir = playerController != null ? playerController.MoveDirection : Vector2.zero;
                return moveDir != Vector2.zero ? moveDir : DirToPlayer();

            case AimMode.Random:
            default:
                Vector2 randomDir = Random.insideUnitCircle.normalized;
                return randomDir != Vector2.zero ? randomDir : DirToPlayer();
        }
    }

    private Vector2[] GetFireDirections(Vector2 baseDir)
    {
        if (firePattern == FirePattern.Straight || radialBulletCount <= 1)
            return new[] { baseDir };

        var directions = new Vector2[radialBulletCount];
        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x);
        float step = Mathf.PI * 2f / radialBulletCount;

        for (int i = 0; i < radialBulletCount; i++)
        {
            float angle = baseAngle + step * i;
            directions[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        return directions;
    }

    private void SpawnProjectile(Vector2 direction, float speed)
    {
        GameObject obj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        obj.GetComponent<EnemyProjectile>().Init(direction, speed, EnemyBalance.RangedProjectileDamage);
    }
}
