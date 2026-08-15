using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sector2Boss : BossBase
{
    private const int ArmorStageIntact = 1;
    private const int ArmorStageDamaged = 2;
    private const int ArmorStageExposed = 3;

    [Header("기믹 - 갑피")]
    [SerializeField] private float armorMaxPool = 60f;
    [SerializeField] private float intactArmorAbsorbRatio = 0.6f;   // 1단계: 60%는 갑피가 흡수, 40%만 본체로
    [SerializeField] private float damagedArmorAbsorbRatio = 0.4f;  // 2단계: 40%는 갑피가 흡수, 60%는 본체로
    [SerializeField] private float exposedDamageMultiplier = 1.3f;  // 3단계: 갑피 없음, 130% 그대로 본체로
    [SerializeField] private Color intactColor = new Color(0.2f, 0.2f, 0.22f);
    [SerializeField] private Color damagedColor = new Color(1f, 0.7f, 0.4f);
    [SerializeField] private Color exposedColor = new Color(1f, 0.3f, 0.3f);

    [Header("파동")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private int waveRepeatCount = 5;
    [SerializeField] private float waveInterval = 0.4f;
    [SerializeField] private float waveSpeed = 7f;
    [SerializeField] private float waveDamage = 8f;

    [Header("바위 던지기")]
    [SerializeField] private int rockCount = 12;
    [SerializeField] private float rockSpeed = 5f;
    [SerializeField] private float rockDamage = 10f;

    [Header("휩쓸기")]
    [SerializeField] private float sweepRange = 2.5f;
    [SerializeField] private float sweepStayThreshold = 2f;
    [SerializeField] private float sweepDuration = 0.8f;
    [SerializeField] private float sweepHitRange = 2.8f;
    [SerializeField] private float sweepDamage = 14f;
    [SerializeField] private float sweepHitCooldown = 0.5f;

    [Header("갑피 재생")]
    [SerializeField] private float regenDurationNormal = 10f;
    [SerializeField] private float regenDurationFromExposed = 4f;

    private int armorStage = ArmorStageIntact;
    private float armorPool;
    private float closeTimer;
    private float lastSweepHitTime = -99f;

    protected override void Awake()
    {
        base.Awake();
        armorPool = armorMaxPool;
        UpdateArmorVisual();
    }

    protected override void Update()
    {
        base.Update();
        TrackCloseTimer();
    }

    private void TrackCloseTimer()
    {
        if (HasPlayer && DistToPlayer() <= sweepRange)
            closeTimer += Time.deltaTime;
        else
            closeTimer = 0f;
    }

    // 한 방이 현재 갑피 단계를 깨고 남을 만큼 크면, 남은 피해를 다음 단계로 계속 흘려보낸다
    // (예전엔 armorPool이 음수여도 다음 단계에서 그냥 꽉 채워버려서 초과분이 사라졌음).
    protected override float ModifyIncomingDamage(float amount)
    {
        float remaining = amount;
        float coreDamage = 0f;

        while (remaining > 0f && armorStage < ArmorStageExposed)
        {
            float armorRatio = armorStage == ArmorStageIntact ? intactArmorAbsorbRatio : damagedArmorAbsorbRatio;
            float incomingToBreak = armorPool / Mathf.Max(armorRatio, Mathf.Epsilon);
            float appliedIncoming = Mathf.Min(remaining, incomingToBreak);

            armorPool -= appliedIncoming * armorRatio;
            coreDamage += appliedIncoming * (1f - armorRatio);
            remaining -= appliedIncoming;

            if (armorPool <= Mathf.Epsilon)
            {
                armorStage++;
                armorPool = armorStage < ArmorStageExposed ? armorMaxPool : 0f;
                UpdateArmorVisual();
            }
        }

        return coreDamage + remaining * exposedDamageMultiplier;
    }

    private void UpdateArmorVisual()
    {
        SetBaseTint(armorStage switch
        {
            ArmorStageIntact => intactColor,
            ArmorStageDamaged => damagedColor,
            _ => exposedColor,
        });
    }

    protected override List<BossPattern> BuildPatterns()
    {
        return new List<BossPattern>
        {
            new BossPattern { Name = "파동", Weight = 1f, Execute = Pattern_Wave },
            new BossPattern { Name = "바위 던지기", Weight = 1f, Execute = Pattern_RockThrow },
            new BossPattern { Name = "휩쓸기", Weight = 1f, CanUse = CanUseSweep, Execute = Pattern_Sweep },
            new BossPattern { Name = "갑피 재생", Weight = 1f, CanUse = CanUseArmorRegen, Execute = Pattern_ArmorRegen },
        };
    }

    private bool CanUseSweep() => closeTimer >= sweepStayThreshold;

    private bool CanUseArmorRegen() => armorStage != ArmorStageIntact;

    private IEnumerator Pattern_Wave()
    {
        if (projectilePrefab == null) yield break;

        for (int i = 0; i < waveRepeatCount; i++)
        {
            if (!HasPlayer) yield break;

            Vector2 dir = DirToPlayer();
            GameObject obj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            obj.GetComponent<EnemyProjectile>().Init(dir, waveSpeed, waveDamage);

            yield return new WaitForSeconds(waveInterval);
        }
    }

    private IEnumerator Pattern_RockThrow()
    {
        // 참고 영상(Undertale-Asgore)은 밖에서 안으로 들어오지만, 인게임은 중심에서 바깥으로 원형 발사.
        EnemyProjectile.FireRadial(projectilePrefab, transform.position, Vector2.right, rockCount, rockSpeed, rockDamage);
        yield return null;
    }

    private IEnumerator Pattern_Sweep()
    {
        float elapsed = 0f;
        while (elapsed < sweepDuration)
        {
            elapsed += Time.deltaTime;

            if (HasPlayer && DistToPlayer() <= sweepHitRange && Time.time - lastSweepHitTime >= sweepHitCooldown)
            {
                lastSweepHitTime = Time.time;
                playerDamageable?.TakeDamage(sweepDamage);
            }

            yield return null;
        }
    }

    private IEnumerator Pattern_ArmorRegen()
    {
        float duration = armorStage >= ArmorStageExposed ? regenDurationFromExposed : regenDurationNormal;
        yield return new WaitForSeconds(duration);

        armorStage = ArmorStageIntact;
        armorPool = armorMaxPool;
        UpdateArmorVisual();
    }
}
