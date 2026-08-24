using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 대검. "에너지" 자원(최대 100) 운용.
// 평타 3연격(회전이 더 강함) + 에너지 충전 + 강화(다음 행동/3연격 전체 강화) + 방어(패링) + 방출 + 분쇄.
public class GreatSword : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Transform firePoint;   // 없으면 자기 자신 transform (조준 = up 방향)
    [SerializeField] private LayerMask hitLayers;    // 적 레이어

    [Header("에너지")]
    [SerializeField] private float maxEnergy = 100f;
    [SerializeField] private float energyPerHit = 4f;

    [Header("평타 - 공통")]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float castDelay = 0.4f;         // 시전 딜레이(누르고 판정까지)
    [SerializeField] private float betweenComboDelay = 0.5f; // 시전 완료 시간(판정 후 다음 입력까지)
    [SerializeField] private float comboResetTime = 1.2f;    // 이 시간 넘게 안 치면 콤보 리셋

    [Header("평타 - 횡베기(1,2타, 초승달)")]
    [SerializeField] private float crescentRadius = 1.5f;
    [SerializeField] private float crescentForwardOffset = 1.0f;
    [SerializeField] private float crescentHalfAngle = 60f;
    [SerializeField] private float stepDistance = 0.5f;      // 1보 (2보 = ×2)

    [Header("평타 - 회전(3타, 원)")]
    [SerializeField] private float spinRadius = 2.0f;
    [SerializeField] private float spinDamageMultiplier = 1.5f; // 회전은 조금 더 강함

    [Header("강화 (1번 키)")]
    [SerializeField] private KeyCode enhanceKey = KeyCode.Alpha1;
    [SerializeField] private float enhanceDamageMax = 2f;       // 강화 시 피해 최대 배율 (에너지 100 → 2배)
    [SerializeField] private float enhanceCastMinFactor = 0.5f; // 강화 시 시전시간 최소 배율 (에너지 100 → 0.5)
    [SerializeField] private Color enhancedColor = Color.magenta;

    [Header("방어 (2번 키) - 강화 미소비(공격 행동에만 적용)")]
    [SerializeField] private KeyCode defendKey = KeyCode.Alpha2;
    [SerializeField] private float defendDuration = 1.5f;   // 패링 판정 시간
    [SerializeField] private float defendCooldown = 2f;
    [SerializeField] private float defendEnergyGain = 20f;  // 패링 성공 시 충전량
    [SerializeField] private float defendSpeedMultiplier = 1f;
    [SerializeField] private Color defendColor = new Color(0.3f, 0.8f, 1f);
    [SerializeField] private Color parrySuccessColor = Color.white;
    [SerializeField] private float defendRingRadius = 1.2f;

    [Header("방출 (3번 키)")]
    [SerializeField] private KeyCode releaseKey = KeyCode.Alpha3;
    [SerializeField] private float releaseDamage = 30f;
    [SerializeField] private float releaseCastTime = 1.2f;   // 시전 딜레이
    [SerializeField] private float releaseCooldown = 6f;
    [SerializeField] private float releaseWidth = 2f;
    [SerializeField] private float releaseLength = 4f;
    [SerializeField] private Color releaseColor = new Color(1f, 0.4f, 0.1f);

    [Header("분쇄 (4번 키)")]
    [SerializeField] private KeyCode crushKey = KeyCode.Alpha4;
    [SerializeField] private float crushDelay = 0.4f;
    [SerializeField] private float crushCastTime = 2.0f;      // 시전 완료 시간(강화 시 에너지 100 → 1초)
    [SerializeField] private float crushCooldown = 8f;
    [SerializeField] private float crushRadius = 1.0f;
    [SerializeField] private float crushForwardOffset = 1.0f;
    [SerializeField] private int crushStompCount = 5;
    [SerializeField] private float crushStompDamage = 8f;
    [SerializeField] private float crushFinisherDamage = 30f; // 마무리가 더 강함
    [SerializeField] private float crushFinisherRadiusMul = 1.5f;
    [SerializeField] private Color crushColor = new Color(0.7f, 0.4f, 0.15f);

    [Header("개발용 화면 표시 (테스트용, 정식 UI는 김세원님)")]
    [SerializeField] private bool showDebugHUD = true;

    private Transform Aim => firePoint != null ? firePoint : transform;

    private float currentEnergy;
    private int comboIndex;          // 0=좌횡, 1=우횡, 2=회전
    private bool isAttacking;
    private float lastSwingTime;
    private Rigidbody2D rb;
    private SwingVisualizer visualizer;

    private bool isEnhanced;              // 강화 대기 중(다음 행동)
    private float capturedEnergy;         // 강화 시 캡처한 에너지
    private bool comboEnhanced;           // 현재 3연격 사이클이 강화 상태인지
    private float comboEnhancedEnergy;    // 그 사이클의 강화 에너지

    private float lastReleaseTime = -999f;
    private float lastCrushTime = -999f;

    private PlayerStats stats;
    private PlayerController controller;
    private Func<float, bool> interceptor;
    private bool isDefending;
    private float lastDefendTime = -999f;
    private float prevSpeedMultiplier = 1f;

    private static readonly Color[] comboColors =
    {
        new Color(0.3f, 0.5f, 1f),
        Color.cyan,
        Color.yellow,
    };

    // ===== UI 노출용 (김세원님) =====
    public float MaxEnergy => maxEnergy;
    public float CurrentEnergy => currentEnergy;
    public float EnergyRatio => maxEnergy > 0f ? currentEnergy / maxEnergy : 0f;
    public bool IsEnhanced => isEnhanced;
    public float ReleaseCooldownRemaining => Mathf.Max(0f, lastReleaseTime + releaseCooldown - Time.time);
    public float ReleaseCooldownRatio => releaseCooldown > 0f ? Mathf.Clamp01(ReleaseCooldownRemaining / releaseCooldown) : 0f;
    public bool IsReleaseReady => Time.time >= lastReleaseTime + releaseCooldown;
    public bool IsDefending => isDefending;
    public float DefendCooldownRemaining => Mathf.Max(0f, lastDefendTime + defendCooldown - Time.time);
    public float DefendCooldownRatio => defendCooldown > 0f ? Mathf.Clamp01(DefendCooldownRemaining / defendCooldown) : 0f;
    public bool IsDefendReady => Time.time >= lastDefendTime + defendCooldown;
    public float CrushCooldownRemaining => Mathf.Max(0f, lastCrushTime + crushCooldown - Time.time);
    public float CrushCooldownRatio => crushCooldown > 0f ? Mathf.Clamp01(CrushCooldownRemaining / crushCooldown) : 0f;
    public bool IsCrushReady => Time.time >= lastCrushTime + crushCooldown;
    // ================================

    // 강화 배율 계산 (에너지 비례)
    private float EnergyRatioOf(float energy) => Mathf.Clamp01(energy / Mathf.Max(1f, maxEnergy));
    private float EnhanceDamageFactor(float energy) => Mathf.Lerp(1f, enhanceDamageMax, EnergyRatioOf(energy));   // 1~2배
    private float EnhanceCastFactor(float energy) => Mathf.Lerp(1f, enhanceCastMinFactor, EnergyRatioOf(energy)); // 1~0.5

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<PlayerStats>();
        controller = GetComponent<PlayerController>();
        interceptor = InterceptDamage;

        if (stats == null)
            Debug.LogWarning("[GreatSword] PlayerStats가 없어 방어(패링)가 동작하지 않습니다. 같은 오브젝트에 있는지 확인하세요.");

        visualizer = GetComponent<SwingVisualizer>();
        if (visualizer == null)
            visualizer = gameObject.AddComponent<SwingVisualizer>();
    }

    private void OnEnable()
    {
        if (stats != null)
            stats.DamageInterceptor = interceptor;
        isAttacking = false;
    }

    private void OnDisable()
    {
        // 내가 등록한 훅일 때만 해제 (다른 무기가 덮어썼으면 유지)
        if (stats != null && stats.DamageInterceptor == interceptor)
            stats.DamageInterceptor = null;

        EndDefend();

        // 대시/강화 등으로 남을 수 있는 상태 정리 (무기 교체 안전)
        if (controller != null) controller.SpeedMultiplier = 1f;
        isEnhanced = false;
        comboEnhanced = false;
        isAttacking = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(enhanceKey)) TryEnhance();
        if (Input.GetKeyDown(defendKey)) TryDefend();
        if (Input.GetKeyDown(releaseKey)) TryRelease();
        if (Input.GetKeyDown(crushKey)) TryCrush();

        if (Input.GetMouseButtonDown(0) && CanSwing())
        {
            // 콤보 리셋 창 지났으면 처음부터 + 강화 해제
            if (Time.time > lastSwingTime + comboResetTime)
            {
                comboIndex = 0;
                comboEnhanced = false;
            }

            // 강화는 3연격 사이클 시작(0타)에 소비되어 사이클 전체에 적용
            if (comboIndex == 0 && ConsumeEnhance(out float e))
            {
                comboEnhanced = true;
                comboEnhancedEnergy = e;
            }

            StartCoroutine(SwingRoutine(comboIndex));
        }
    }

    private bool CanSwing()
    {
        float gap = comboEnhanced ? betweenComboDelay * EnhanceCastFactor(comboEnhancedEnergy) : betweenComboDelay;
        return !isAttacking && Time.time >= lastSwingTime + gap;
    }

    private IEnumerator SwingRoutine(int index)
    {
        isAttacking = true;

        bool isSpin = (index == 2);
        Vector2 aimDir = Aim.up;

        float baseDmg = attackDamage * (isSpin ? spinDamageMultiplier : 1f);
        float dmg = comboEnhanced ? baseDmg * EnhanceDamageFactor(comboEnhancedEnergy) : baseDmg;
        float delay = comboEnhanced ? castDelay * EnhanceCastFactor(comboEnhancedEnergy) : castDelay;

        // 횡베기(1,2타)는 공격 방향으로 2보 이동
        if (!isSpin && rb != null)
            StartCoroutine(StepDashRoutine(aimDir, stepDistance * 2f, delay));

        yield return new WaitForSeconds(delay);

        DoHit(aimDir, index, dmg, comboEnhanced);

        isAttacking = false;
        lastSwingTime = Time.time;
        comboIndex = (index + 1) % 3;

        // 3연격 완료 → 강화 사이클 종료
        if (comboIndex == 0)
            comboEnhanced = false;
    }

    // 공격 방향 2보 대시. 대시 중엔 SpeedMultiplier=0으로 PlayerController 이동을 잠가 충돌 방지.
    private IEnumerator StepDashRoutine(Vector2 dir, float distance, float duration)
    {
        if (duration <= 0f)
        {
            rb.MovePosition(rb.position + dir.normalized * distance);
            yield break;
        }

        float prevMul = controller != null ? controller.SpeedMultiplier : 1f;
        if (controller != null) controller.SpeedMultiplier = 0f;

        Vector2 start = rb.position;
        Vector2 target = start + dir.normalized * distance;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.fixedDeltaTime;
            rb.MovePosition(Vector2.Lerp(start, target, elapsed / duration));
            yield return new WaitForFixedUpdate();
        }

        if (controller != null) controller.SpeedMultiplier = prevMul;
    }

    private void DoHit(Vector2 aimDir, int index, float damage, bool enhanced)
    {
        bool isSpin = (index == 2);
        Vector2 selfPos = transform.position;
        Vector2 center = isSpin ? selfPos : selfPos + aimDir.normalized * crescentForwardOffset;
        float radius = isSpin ? spinRadius : crescentRadius;

        if (visualizer != null)
        {
            Color c = enhanced ? enhancedColor : comboColors[Mathf.Clamp(index, 0, comboColors.Length - 1)];
            visualizer.FlashCircle(center, radius, c);
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, hitLayers);
        HashSet<IDamageable> damaged = new(); // 적 단위 중복 타격 방지

        foreach (Collider2D hit in hits)
        {
            if (!isSpin)
            {
                Vector2 toTarget = ((Vector2)hit.transform.position - selfPos).normalized;
                if (Vector2.Angle(aimDir, toTarget) > crescentHalfAngle)
                    continue;
            }

            if (hit.TryGetComponent<IDamageable>(out var target) && !target.IsDead && damaged.Add(target))
            {
                target.TakeDamage(damage);
                AddEnergy(energyPerHit);
            }
        }
    }

    private void AddEnergy(float amount)
    {
        currentEnergy = Mathf.Min(maxEnergy, currentEnergy + amount);
    }

    // 강화: 저장 에너지를 전부 소모하고 "다음 행동 강화" 상태 ON. 쿨타임 없음.
    private void TryEnhance()
    {
        if (isEnhanced) return;
        if (currentEnergy <= 0f) return;

        capturedEnergy = currentEnergy;
        currentEnergy = 0f;
        isEnhanced = true;
    }

    // 강화 상태면 소비하며 true 반환. energy엔 캡처 에너지량(배율 계산용).
    private bool ConsumeEnhance(out float energy)
    {
        if (isEnhanced)
        {
            isEnhanced = false;
            energy = capturedEnergy;
            capturedEnergy = 0f;
            return true;
        }
        energy = 0f;
        return false;
    }

    // 방어: 다음 피해를 막아낸다. 패링 1.5초, 성공 시 즉시 종료 + 에너지 +20. 쿨타임 2초.
    // 방어는 강화를 소비하지 않는다(강화는 공격 행동 전용).
    private void TryDefend()
    {
        if (isAttacking) return;
        if (Time.time < lastDefendTime + defendCooldown) return;
        StartCoroutine(DefendRoutine());
    }

    private IEnumerator DefendRoutine()
    {
        isAttacking = true;
        isDefending = true;
        lastDefendTime = Time.time;

        if (controller != null)
        {
            prevSpeedMultiplier = controller.SpeedMultiplier;
            controller.SpeedMultiplier = defendSpeedMultiplier;
        }

        float endTime = Time.time + defendDuration;
        while (isDefending && Time.time < endTime)
        {
            if (visualizer != null)
                visualizer.FlashCircle(transform.position, defendRingRadius, defendColor);
            yield return null;
        }

        EndDefend();
        isAttacking = false;
    }

    private void EndDefend()
    {
        if (!isDefending) return;
        isDefending = false;
        if (controller != null)
            controller.SpeedMultiplier = prevSpeedMultiplier;
    }

    // PlayerStats.TakeDamage에서 호출되는 훅. true 반환 시 그 피해는 무시된다.
    private bool InterceptDamage(float amount)
    {
        if (!isDefending) return false;

        EndDefend();
        AddEnergy(defendEnergyGain);
        if (visualizer != null)
            visualizer.FlashCircle(transform.position, defendRingRadius, parrySuccessColor);
        return true;
    }

    // 방출: 1.2초 시전 후 전방 직사각형 피해. 강화 시 피해↑(최대 2배)·시전시간↓. 쿨타임 6초.
    private void TryRelease()
    {
        if (isAttacking) return;
        if (Time.time < lastReleaseTime + releaseCooldown) return;
        StartCoroutine(ReleaseRoutine());
    }

    private IEnumerator ReleaseRoutine()
    {
        isAttacking = true;
        lastReleaseTime = Time.time;

        Vector2 aimDir = Aim.up;

        bool enhanced = ConsumeEnhance(out float energy);
        float castTime = enhanced ? releaseCastTime * EnhanceCastFactor(energy) : releaseCastTime;
        float dmg = enhanced ? releaseDamage * EnhanceDamageFactor(energy) : releaseDamage;

        yield return new WaitForSeconds(castTime);

        DoBoxHit(aimDir, dmg, enhanced);
        isAttacking = false;
    }

    private void DoBoxHit(Vector2 aimDir, float damage, bool enhanced)
    {
        Vector2 forward = aimDir.normalized;
        Vector2 selfPos = transform.position;
        Vector2 center = selfPos + forward * (releaseLength * 0.5f);
        Vector2 size = new Vector2(releaseWidth, releaseLength);
        float angle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg - 90f;

        if (visualizer != null)
            visualizer.FlashBox(center, size, angle, enhanced ? enhancedColor : releaseColor);

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, angle, hitLayers);
        HashSet<IDamageable> damaged = new();
        foreach (Collider2D hit in hits)
            if (hit.TryGetComponent<IDamageable>(out var target) && !target.IsDead && damaged.Add(target))
                target.TakeDamage(damage);
    }

    // 분쇄: 0.4초 후 앞 작은 원에 찍기×N → 마무리(더 강함). 전체 2초(강화 시 에너지100 → 1초). 쿨타임 8초.
    private void TryCrush()
    {
        if (isAttacking) return;
        if (Time.time < lastCrushTime + crushCooldown) return;
        StartCoroutine(CrushRoutine());
    }

    private IEnumerator CrushRoutine()
    {
        isAttacking = true;
        lastCrushTime = Time.time;

        Vector2 forward = Aim.up.normalized;

        bool enhanced = ConsumeEnhance(out float energy);
        float total = enhanced ? crushCastTime * EnhanceCastFactor(energy) : crushCastTime;
        float dmgFactor = enhanced ? EnhanceDamageFactor(energy) : 1f;
        Color color = enhanced ? enhancedColor : crushColor;

        yield return new WaitForSeconds(crushDelay);

        // 찍기 사이 간격 = 남은 시간 / N → 마무리가 정확히 시전 끝(total)에 발생.
        float stompPhase = Mathf.Max(0.1f, total - crushDelay);
        float interval = stompPhase / Mathf.Max(1, crushStompCount);

        for (int i = 0; i < crushStompCount; i++)
        {
            Vector2 c = (Vector2)transform.position + forward * crushForwardOffset;
            DoCircleDamage(c, crushRadius, crushStompDamage * dmgFactor, color);
            yield return new WaitForSeconds(interval);
        }

        Vector2 fc = (Vector2)transform.position + forward * crushForwardOffset;
        DoCircleDamage(fc, crushRadius * crushFinisherRadiusMul, crushFinisherDamage * dmgFactor, color);

        isAttacking = false;
    }

    private void DoCircleDamage(Vector2 center, float radius, float damage, Color color)
    {
        if (visualizer != null)
            visualizer.FlashCircle(center, radius, color);

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, hitLayers);
        HashSet<IDamageable> damaged = new();
        foreach (Collider2D hit in hits)
            if (hit.TryGetComponent<IDamageable>(out var target) && !target.IsDead && damaged.Add(target))
                target.TakeDamage(damage);
    }

    private void OnGUI()
    {
        if (!showDebugHUD) return;

        var style = new GUIStyle(GUI.skin.label) { fontSize = 20 };
        style.normal.textColor = Color.white;

        string text =
            $"[대검]\n" +
            $"에너지: {currentEnergy:F0} / {maxEnergy:F0}\n" +
            $"강화: {(isEnhanced ? "ON" : "-")}{(comboEnhanced ? " (평타적용중)" : "")}\n" +
            $"방어: {(isDefending ? "판정 중" : "-")} (쿨 {DefendCooldownRemaining:F1}s)\n" +
            $"방출 쿨: {ReleaseCooldownRemaining:F1}s\n" +
            $"분쇄 쿨: {CrushCooldownRemaining:F1}s";

        GUI.Label(new Rect(14, 12, 400, 220), text, style);
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 selfPos = transform.position;
        Vector2 aimDir = Aim.up;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(selfPos + aimDir.normalized * crescentForwardOffset, crescentRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(selfPos, spinRadius);
    }
}
