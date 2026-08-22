using UnityEngine;

// 3섹터 보스 "소환" 패턴 전용 잡몹. 느린 일반형(MeleeNormalMovingEnemy)을 그대로 재사용하고,
// 죽었을 때 바닥에 피해 장판을 남기는 것만 추가한다.
public class SummonedAdds : MeleeNormalMovingEnemy
{
    [Header("사망 시 장판")]
    [SerializeField] private float deathZoneRadius = 1.2f;
    [SerializeField] private float deathZoneTelegraph = 0.3f;
    [SerializeField] private float deathZoneDamage = 8f;

    protected override void OnDied()
    {
        var zoneGo = new GameObject("DamageZone");
        zoneGo.transform.position = transform.position;
        zoneGo.AddComponent<DamageZone>().Init(deathZoneRadius, deathZoneTelegraph, deathZoneDamage);

        base.OnDied();
    }
}
