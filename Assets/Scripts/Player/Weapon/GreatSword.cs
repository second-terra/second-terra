using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 대검. "에너지" 자원(최대 100) 운용.
// 평타 3연격 + 에너지 충전 + 강화 + 방어(패링) + 방출(전방 사각형) + 분쇄(찍기 연타).
public class GreatSword : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Transform firePoint;   // 없으면 자기 자신 transform 사용 (조준 = up 방향)
    [SerializeField] private LayerMask hitLayers;    // 적 레이어 (PlayerCombat과 동일하게 세팅)

    [Header("에너지")]
    [SerializeField] private float maxEnergy = 100f;
    [SerializeField] private float energyPerHit = 2f;
    private float currentEnergy;

    [Header("평타 - 공통")]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float castDelay = 0.17f;       // 시전 딜레이(선딜: 휘두르고 판정까지)
    [SerializeField] private float betweenComboDelay = 0.3f; // 각 공격 간 딜레이
    [SerializeField] private float comboResetTime = 1.0f;    // 이 시간 넘게 안 치면 콤보 리셋

    [Header("평타 - 횡베기(1,2타, 초승달)")]
    [SerializeField] private float crescentRadius = 1.5f;    // 판정 반경
    [SerializeField] private float crescentForwardOffset = 1.0f; // 캐릭터 앞으로 얼마나
    [SerializeField] private float crescentHalfAngle = 60f;  // 초승달 반각(전체 120도)
    [SerializeField] private float stepDistance = 0.5f;      // 1보 거리 (2보 = ×2)

    [Header("평타 - 회전(3타, 원)")]
    [SerializeField] private float spinRadius = 2.0f;

    [Header("강화 (1번 키)")]
    [SerializeField] private KeyCode enhanceKey = KeyCode.Alpha1;
    [SerializeField] private float enhanceDamageMultiplier = 2f;  // 강화 시 피해 배율
    [SerializeField] private float enhanceCastMultiplier = 0.5f;  // 강화 시 시전 딜레이 배율(짧아짐)
    [SerializeField] private Color enhancedColor = Color.magenta; // 강화된 공격 히트박스 색

    [Header("방어 (2번 키)")]
    [SerializeField] private KeyCode defendKey = KeyCode.Alpha2;
    [SerializeField] private float defendDuration = 1.5f;   // 패링 판정 시간
    [SerializeField] private float defendCooldown = 2f;
    [SerializeField] private float defendEnergyGain = 20f;  // 패링 성공 시 충전량
    [SerializeField] private float defendSpeedMultiplier = 1f; // 시전 중 이동속도 배율(1=제한 없음)
    [SerializeField] private Color defendColor = new Color(0.3f, 0.8f, 1f);   // 방어 자세 링(하늘)
    [SerializeField] private Color parrySuccessColor = Color.white;           // 패링 성공 링
    [SerializeField] private float defendRingRadius = 1.2f;

    [Header("방출 (3번 키)")]
    [SerializeField] private KeyCode releaseKey = KeyCode.Alpha3;
    [SerializeField] private float releaseDamage = 30f;      // 기본 피해량
    [SerializeField] private float releaseCastTime = 1.6f;   // 시전 시간(딜레이)
    [SerializeField] private float releaseCooldown = 6f;
    [SerializeField] private float releaseWidth = 2f;        // 직사각형 폭(좌우)
    [SerializeField] private float releaseLength = 4f;       // 직사각형 길이(전방)
    [SerializeField] private float enhanceEnergyDamageMul = 2f;          // 강화 시 에너지×N 피해 증가
    [SerializeField] private float releaseEnhanceMinCastFactor = 0.25f;  // 강화+에너지100 시 시전시간 배율
    [SerializeField] private Color releaseColor = new Color(1f, 0.4f, 0.1f); // 방출 히트박스 색(주황)

    [Header("분쇄 (4번 키)")]
    [SerializeField] private KeyCode crushKey = KeyCode.Alpha4;
    [SerializeField] private float crushDelay = 0.4f;         // 초기 딜레이(선딜)
    [SerializeField] private float crushCastTime = 2.0f;      // 시전 완료 시간(전체)
    [SerializeField] private float crushCooldown = 8f;
    [SerializeField] private float crushRadius = 1.0f;        // 작은 원형 반경
    [SerializeField] private float crushForwardOffset = 1.0f; // 바로 앞 거리
    [SerializeField] private int crushStompCount = 5;         // 찍기 횟수
    [SerializeField] private float crushStompDamage = 8f;     // 찍기 1회 피해
    [SerializeField] private float crushFinisherDamage = 30f; // 마무리 찍기 피해
    [SerializeField] private float crushFinisherRadiusMul = 1.5f; // 마무리 반경 배율
    [SerializeField] private float crushEnhanceMinCastFactor = 0.5f; // 강화+에너지100 시 시전시간 배율(2s→1s)
    [SerializeField] private Color crushColor = new Color(0.7f, 0.4f, 0.15f); // 분쇄 히트박스 색(갈색)

    [Header("개발용 화면 표시 (테스트용, 정식 UI는 김세원님)")]
    [SerializeField] private bool showDebugHUD = true;

    private Transform Aim => firePoint != null ? firePoint : transform;

    private int comboIndex;          // 0=좌횡, 1=우횡, 2=회전
    private bool isAttacking;
    private float lastSwingTime;
    private Rigidbody2D rb;
    private SwingVisualizer visualizer;

    private bool isEnhanced;         // 다음 행동이 강화 상태인지
    private float capturedEnergy;    // 강화 시전 시 캡처한 에너지 (스킬 배율용)
    private float lastReleaseTime = -999f; // 방출 쿨타임 기준
    private float lastCrushTime = -999f;   // 분쇄 쿨타임 기준

    private PlayerStats stats;             // 피해 가로채기 훅(DamageInterceptor) 등록용
    private PlayerController controller;   // 시전 중 이동속도 조절용
    private Func<float, bool> interceptor; // 등록한 훅 참조 (해제할 때 내 것인지 확인용)
    private bool isDefending;              // 패링 판정 창이 열려 있는지
    private float lastDefendTime = -999f;  // 방어 쿨타임 기준
    private float prevSpeedMultiplier = 1f; // 방어 시전 전 이동속도 배율 (복구용)

    // 콤보 단계별 히트박스 색 (1타 파랑 / 2타 하늘 / 3타 노랑)
    private static readonly Color[] comboColors =
    {
        new Color(0.3f, 0.5f, 1f),
        Color.cyan,
        Color.yellow,
    };

    // ===== UI 노출용 (김세원님 UI에서 읽어감) =====
    // 에너지
    public float MaxEnergy => maxEnergy;
    public float CurrentEnergy => currentEnergy;
    public float EnergyRatio => maxEnergy > 0f ? currentEnergy / maxEnergy : 0f; // 0~1 (바 채움용)
    public bool IsEnhanced => isEnhanced;                                        // 강화 대기 중?

    // 방출(3) 쿨타임: 남은 초 / 0~1 비율(1=방금 씀, 0=준비됨) / 준비 여부
    public float ReleaseCooldownRemaining => Mathf.Max(0f, lastReleaseTime + releaseCooldown - Time.time);
    public float ReleaseCooldownRatio => releaseCooldown > 0f ? Mathf.Clamp01(ReleaseCooldownRemaining / releaseCooldown) : 0f;
    public bool IsReleaseReady => Time.time >= lastReleaseTime + releaseCooldown;

    // 방어(2) 쿨타임 + 패링 판정 중 여부
    public bool IsDefending => isDefending;
    public float DefendCooldownRemaining => Mathf.Max(0f, lastDefendTime + defendCooldown - Time.time);
    public float DefendCooldownRatio => defendCooldown > 0f ? Mathf.Clamp01(DefendCooldownRemaining / defendCooldown) : 0f;
    public bool IsDefendReady => Time.time >= lastDefendTime + defendCooldown;

    // 분쇄(4) 쿨타임
    public float CrushCooldownRemaining => Mathf.Max(0f, lastCrushTime + crushCooldown - Time.time);
    public float CrushCooldownRatio => crushCooldown > 0f ? Mathf.Clamp01(CrushCooldownRemaining / crushCooldown) : 0f;
    public bool IsCrushReady => Time.time >= lastCrushTime + crushCooldown;
    // =========================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<PlayerStats>();
        controller = GetComponent<PlayerController>();
        interceptor = InterceptDamage;

        if (stats == null)
            Debug.LogWarning("[GreatSword] PlayerStats를 찾지 못해 방어(패링)가 동작하지 않습니다. 같은 오브젝트에 있는지 확인하세요.");

        // 히트박스 시각화 컴포넌트 자동 확보 (없으면 붙임)
        visualizer = GetComponent<SwingVisualizer>();
        if (visualizer == null)
            visualizer = gameObject.AddComponent<SwingVisualizer>();
    }

    // 패링용 피해 가로채기 훅 등록/해제.
    // 무기 교체 시 컴포넌트가 켜지고 꺼지므로 여기서 붙였다 떼는 게 안전하다.
    private void OnEnable()
    {
        if (stats != null)
            stats.DamageInterceptor = interceptor;

        // 비활성화되면 진행 중이던 코루틴이 중단되어 행동 잠금이 남을 수 있으므로 여기서 푼다
        isAttacking = false;
    }

    private void OnDisable()
    {
        // 내가 등록한 훅일 때만 해제 (다른 무기가 이미 덮어썼으면 건드리지 않음)
        if (stats != null && stats.DamageInterceptor == interceptor)
            stats.DamageInterceptor = null;

        EndDefend();
    }

    private void Update()
    {
        // 강화 (1번 키): 다음 행동을 강화
        if (Input.GetKeyDown(enhanceKey))
            TryEnhance();

        // 방어 (2번 키)
        if (Input.GetKeyDown(defendKey))
            TryDefend();

        // 방출 (3번 키)
        if (Input.GetKeyDown(releaseKey))
            TryRelease();

        // 분쇄 (4번 키)
        if (Input.GetKeyDown(crushKey))
            TryCrush();

        if (Input.GetMouseButtonDown(0) && CanSwing())
        {
            // 콤보 리셋 창 지났으면 처음부터
            if (Time.time > lastSwingTime + comboResetTime)
                comboIndex = 0;

            StartCoroutine(SwingRoutine(comboIndex));
        }
    }

    private bool CanSwing()
    {
        return !isAttacking && Time.time >= lastSwingTime + betweenComboDelay;
    }

    private IEnumerator SwingRoutine(int index)
    {
        isAttacking = true;

        bool isSpin = (index == 2);
        Vector2 aimDir = Aim.up;   // 스윙 시작 시점 방향 고정

        // 강화 소비: 이 스윙 1회에 적용 (피해↑, 시전 딜레이↓)
        bool enhanced = ConsumeEnhance(out _);
        float dmg = enhanced ? attackDamage * enhanceDamageMultiplier : attackDamage;
        float delay = enhanced ? castDelay * enhanceCastMultiplier : castDelay;

        // 횡베기(1,2타)는 공격 방향으로 2보 이동
        if (!isSpin && rb != null)
            StartCoroutine(StepDashRoutine(aimDir, stepDistance * 2f, delay));

        // 선딜
        yield return new WaitForSeconds(delay);

        // 히트 판정
        DoHit(aimDir, index, dmg, enhanced);

        isAttacking = false;
        lastSwingTime = Time.time;
        comboIndex = (index + 1) % 3;
    }

    // 공격 방향으로 밀어주는 대시. 주의: PlayerController가 FixedUpdate마다 velocity를 덮어써서
    // 이동키를 누르고 있으면 충돌함. 지금은 MovePosition으로 처리하되, 추후 팀원 훅으로 정리 예정.
    private IEnumerator StepDashRoutine(Vector2 dir, float distance, float duration)
    {
        // duration이 0이면 elapsed/duration이 NaN → MovePosition에 NaN 들어가 물리 파괴됨. 즉시 이동 후 종료.
        if (duration <= 0f)
        {
            rb.MovePosition(rb.position + dir.normalized * distance);
            yield break;
        }

        Vector2 start = rb.position;
        Vector2 target = start + dir.normalized * distance;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.fixedDeltaTime;
            rb.MovePosition(Vector2.Lerp(start, target, elapsed / duration));
            yield return new WaitForFixedUpdate();
        }
    }

    private void DoHit(Vector2 aimDir, int index, float damage, bool enhanced)
    {
        bool isSpin = (index == 2);
        Vector2 selfPos = transform.position;
        Vector2 center = isSpin ? selfPos : selfPos + aimDir.normalized * crescentForwardOffset;
        float radius = isSpin ? spinRadius : crescentRadius;

        // 히트박스 시각화 (개발용) — 강화면 마젠타, 아니면 콤보 단계별 색
        if (visualizer != null)
        {
            Color c = enhanced ? enhancedColor : comboColors[Mathf.Clamp(index, 0, comboColors.Length - 1)];
            visualizer.FlashCircle(center, radius, c);
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, hitLayers);
        HashSet<IDamageable> damaged = new(); // 적 단위 중복 타격 방지(콜라이더 여러 개여도 1회)

        foreach (Collider2D hit in hits)
        {
            // 초승달: 앞쪽 각도 안에 있는 적만
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

    // 강화 시전: 저장된 에너지를 전부 소모하고 "다음 행동 강화" 상태 ON. 쿨타임 없음.
    private void TryEnhance()
    {
        if (isEnhanced) return;               // 이미 강화 대기 중이면 무시
        if (currentEnergy <= 0f)
        {
            Debug.Log("[GreatSword] 강화 실패: 에너지 없음");
            return;
        }

        capturedEnergy = currentEnergy;
        currentEnergy = 0f;
        isEnhanced = true;
        Debug.Log($"[GreatSword] 강화 ON (소모 에너지 {capturedEnergy}) → 다음 행동 강화됨");
    }

    // 강화 상태면 소비하면서 true 반환. energy엔 강화 시 캡처한 에너지량(방출/분쇄 배율용).
    private bool ConsumeEnhance(out float energy)
    {
        if (isEnhanced)
        {
            isEnhanced = false;
            energy = capturedEnergy;
            capturedEnergy = 0f;
            Debug.Log("[GreatSword] 강화 소비됨 (이번 행동 강화 적용)");
            return true;
        }
        energy = 0f;
        return false;
    }

    // 방어: 다음 피해를 막아낸다. 패링 판정 1.5초, 성공 시 즉시 시전 종료 + 에너지 충전. 쿨타임 2초.
    private void TryDefend()
    {
        if (isAttacking) return;
        if (Time.time < lastDefendTime + defendCooldown)
        {
            Debug.Log($"[GreatSword] 방어 쿨타임 {lastDefendTime + defendCooldown - Time.time:F1}초 남음");
            return;
        }
        StartCoroutine(DefendRoutine());
    }

    private IEnumerator DefendRoutine()
    {
        isAttacking = true;
        isDefending = true;
        lastDefendTime = Time.time;   // 쿨타임은 시전 시작 기준 (방출/분쇄와 동일)

        if (controller != null)
        {
            prevSpeedMultiplier = controller.SpeedMultiplier;
            controller.SpeedMultiplier = defendSpeedMultiplier;
        }

        Debug.Log($"[GreatSword] 방어 시전 (판정 {defendDuration:F1}초)");

        float endTime = Time.time + defendDuration;

        // 판정 창이 열려 있는 동안 유지. InterceptDamage가 패링에 성공하면 isDefending을 내려서 즉시 빠져나온다.
        while (isDefending && Time.time < endTime)
        {
            // SwingVisualizer의 표시는 짧게 사라지므로 매 프레임 갱신해 지속 표시처럼 보이게 한다
            if (visualizer != null)
                visualizer.FlashCircle(transform.position, defendRingRadius, defendColor);

            yield return null;
        }

        if (isDefending)
            Debug.Log("[GreatSword] 방어 종료 (막아낸 피해 없음)");

        EndDefend();
        isAttacking = false;
    }

    // 방어 상태 정리 (판정 종료 / 패링 성공 / 컴포넌트 비활성화 공통)
    private void EndDefend()
    {
        // 방어 중이 아닐 때 호출되면(중복 호출·비활성화 등) 아무것도 하지 않는다.
        // 이 가드가 없으면 다른 시스템이 조절 중인 이동속도까지 덮어쓴다.
        if (!isDefending) return;

        isDefending = false;

        if (controller != null)
            controller.SpeedMultiplier = prevSpeedMultiplier;
    }

    // PlayerStats.TakeDamage에서 호출되는 훅. true를 반환하면 그 피해는 무시된다.
    private bool InterceptDamage(float amount)
    {
        if (!isDefending) return false;

        // 패링 성공: 판정 창을 닫아 시전을 끝내고(DefendRoutine이 다음 프레임에 빠져나오며 행동 잠금 해제),
        // 에너지를 충전한다.
        EndDefend();
        AddEnergy(defendEnergyGain);

        if (visualizer != null)
            visualizer.FlashCircle(transform.position, defendRingRadius, parrySuccessColor);

        Debug.Log($"[GreatSword] 패링 성공! 피해 {amount} 무효화, 에너지 +{defendEnergyGain} (현재 {currentEnergy})");
        return true;
    }

    // 방출: 1.6초 시전 후 전방 직사각형 범위 피해. 쿨타임 6초.
    private void TryRelease()
    {
        if (isAttacking) return;
        if (Time.time < lastReleaseTime + releaseCooldown)
        {
            Debug.Log($"[GreatSword] 방출 쿨타임 {lastReleaseTime + releaseCooldown - Time.time:F1}초 남음");
            return;
        }
        StartCoroutine(ReleaseRoutine());
    }

    private IEnumerator ReleaseRoutine()
    {
        isAttacking = true;
        lastReleaseTime = Time.time;   // 쿨타임은 시전 시작 기준

        Vector2 aimDir = Aim.up;

        // 강화 소비: 피해 += 에너지×2, 시전시간 감소(에너지 100일 때 1/4)
        bool enhanced = ConsumeEnhance(out float energy);
        float castTime = enhanced
            ? releaseCastTime * Mathf.Lerp(1f, releaseEnhanceMinCastFactor, energy / Mathf.Max(1f, maxEnergy))
            : releaseCastTime;
        float dmg = enhanced
            ? releaseDamage + energy * enhanceEnergyDamageMul
            : releaseDamage;

        Debug.Log($"[GreatSword] 방출 시전 (강화={enhanced}, 시전시간 {castTime:F2}초, 피해 {dmg})");

        yield return new WaitForSeconds(castTime);

        DoBoxHit(aimDir, dmg, enhanced);

        isAttacking = false;
    }

    // 전방 직사각형 범위 피해 (방출용). 에너지 충전 없음.
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
        HashSet<IDamageable> damaged = new(); // 적 단위 중복 타격 방지

        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target) && !target.IsDead && damaged.Add(target))
                target.TakeDamage(damage);
        }
    }

    // 분쇄: 0.4초 후 바로 앞 작은 원형에 찍기×5 → 마무리 찍기. 전체 2초, 쿨타임 8초.
    private void TryCrush()
    {
        if (isAttacking) return;
        if (Time.time < lastCrushTime + crushCooldown)
        {
            Debug.Log($"[GreatSword] 분쇄 쿨타임 {lastCrushTime + crushCooldown - Time.time:F1}초 남음");
            return;
        }
        StartCoroutine(CrushRoutine());
    }

    private IEnumerator CrushRoutine()
    {
        isAttacking = true;
        lastCrushTime = Time.time;   // 쿨타임은 시전 시작 기준

        Vector2 aimDir = Aim.up;
        Vector2 forward = aimDir.normalized;

        // 강화 소비: 시전 시간 감소(에너지 100일 때 1/2), 마무리 피해 += 에너지×2
        bool enhanced = ConsumeEnhance(out float energy);
        float total = enhanced
            ? crushCastTime * Mathf.Lerp(1f, crushEnhanceMinCastFactor, energy / Mathf.Max(1f, maxEnergy))
            : crushCastTime;
        float finisherBonus = enhanced ? energy * enhanceEnergyDamageMul : 0f;
        Color color = enhanced ? enhancedColor : crushColor;

        Debug.Log($"[GreatSword] 분쇄 시전 (강화={enhanced}, 전체 {total:F2}초, 마무리 피해 {crushFinisherDamage + finisherBonus})");

        // 선딜
        yield return new WaitForSeconds(crushDelay);

        // 찍기 × N 후 마무리. 찍기 사이 간격 = 남은 시간 / N → 마무리가 정확히 시전 끝(total)에 발생.
        float stompPhase = Mathf.Max(0.1f, total - crushDelay);
        float interval = stompPhase / Mathf.Max(1, crushStompCount);

        for (int i = 0; i < crushStompCount; i++)
        {
            Vector2 c = (Vector2)transform.position + forward * crushForwardOffset;
            DoCircleDamage(c, crushRadius, crushStompDamage, color);
            yield return new WaitForSeconds(interval);
        }

        // 마무리 찍기 (더 큰 원 + 강화 보너스)
        Vector2 fc = (Vector2)transform.position + forward * crushForwardOffset;
        DoCircleDamage(fc, crushRadius * crushFinisherRadiusMul, crushFinisherDamage + finisherBonus, color);

        isAttacking = false;
    }

    // 원형 범위 단순 피해 (분쇄용, 각도 필터·에너지 충전 없음)
    private void DoCircleDamage(Vector2 center, float radius, float damage, Color color)
    {
        if (visualizer != null)
            visualizer.FlashCircle(center, radius, color);

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, hitLayers);
        HashSet<IDamageable> damaged = new(); // 적 단위 중복 타격 방지

        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target) && !target.IsDead && damaged.Add(target))
                target.TakeDamage(damage);
        }
    }

    // 개발용 화면 표시 (테스트로 에너지/쿨타임 눈으로 확인. 정식 UI는 김세원님)
    private void OnGUI()
    {
        if (!showDebugHUD) return;

        var style = new GUIStyle(GUI.skin.label) { fontSize = 20 };
        style.normal.textColor = Color.white;

        string text =
            $"[대검]\n" +
            $"에너지: {currentEnergy:F0} / {maxEnergy:F0}\n" +
            $"강화: {(isEnhanced ? "ON" : "-")}\n" +
            $"방어: {(isDefending ? "판정 중" : "-")} (쿨 {DefendCooldownRemaining:F1}s)\n" +
            $"방출 쿨: {ReleaseCooldownRemaining:F1}s\n" +
            $"분쇄 쿨: {CrushCooldownRemaining:F1}s";

        GUI.Label(new Rect(14, 12, 400, 220), text, style);
    }

    // 씬 뷰에서 히트박스 확인용
    private void OnDrawGizmosSelected()
    {
        Transform aim = Aim;
        Vector2 selfPos = transform.position;
        Vector2 aimDir = aim.up;

        // 초승달(횡베기)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(selfPos + aimDir.normalized * crescentForwardOffset, crescentRadius);

        // 회전(원)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(selfPos, spinRadius);
    }
}
