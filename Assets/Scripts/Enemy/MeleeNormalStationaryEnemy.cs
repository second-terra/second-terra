public class MeleeNormalStationaryEnemy : MeleeNormalEnemyBase
{
    protected override void UpdateMovementWhileAttacking()
    {
        StopMoving();
    }
}
