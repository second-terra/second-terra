using UnityEngine;

public class MeleeNormalMovingEnemy : MeleeNormalEnemyBase
{
    [Header("타격 중 이동")]
    [SerializeField] private float attackMoveSpeedMultiplier = 0.5f;

    protected override void UpdateMovementWhileAttacking()
    {
        Move(DirToPlayer(), moveSpeed * attackMoveSpeedMultiplier);
    }
}
