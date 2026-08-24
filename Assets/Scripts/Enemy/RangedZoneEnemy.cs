using UnityEngine;

public class RangedZoneEnemy : RangedEnemyBase
{
    public enum ZoneLocation { RandomArea, AroundPlayer }

    [Header("장판")]
    [SerializeField] private ZoneLocation zoneLocation = ZoneLocation.RandomArea;
    [SerializeField] private float zoneRadius = 1.5f;
    [SerializeField] private float telegraphDuration = 1f;
    [SerializeField] private float spawnRadius = 4f;
    [SerializeField] private float aroundPlayerRadius = 2f;

    protected override void PerformAttack(IDamageable target)
    {
        Vector2 spawnPos = GetSpawnPosition();

        var zoneGo = new GameObject("DamageZone");
        zoneGo.transform.position = spawnPos;
        zoneGo.AddComponent<DamageZone>().Init(zoneRadius, telegraphDuration, EnemyBalance.RangedZoneDamage);
    }

    private Vector2 GetSpawnPosition()
    {
        switch (zoneLocation)
        {
            case ZoneLocation.AroundPlayer:
                return (Vector2)playerTransform.position + Random.insideUnitCircle * aroundPlayerRadius;

            case ZoneLocation.RandomArea:
            default:
                return (Vector2)transform.position + Random.insideUnitCircle * spawnRadius;
        }
    }
}
