using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 기관단총 4정 + 기계팔. 히트스캔 원거리 무기(사정거리 있음, 선이 사정거리 지나면 소멸).
// 사격(좌클릭) + 탄창 60발 + 재장전 + 탄막(1) + 도탄(2) + 과부하(3).
public class SubmachineGun : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Transform firePoint;   // 없으면 자기 자신 (조준 = up = 커서 방향)
    [SerializeField] private LayerMask hitLayers;    // 적 레이어

    [Header("사격")]
    [SerializeField] private float attackDamage = 5f;
    [SerializeField] private float attackRange = 15f;   // 사정거리
    [SerializeField] private float rpm = 720f;          // 분당 발사수 (720 = 초당 12발)

    [Header("탄창")]
    [SerializeField] private int magSize = 60;
    [SerializeField] private float reloadTime = 1f;

    [Header("탄막 (1키, 토글)")]
    [SerializeField] private KeyCode barrageKey = KeyCode.Alpha1;
    [SerializeField] private float barrageDamage = 4f;
    [SerializeField] private float barrageRadius = 3f;   // 캐릭터 중심 원 범위
    [SerializeField] private float barrageRpm = 300f;    // 초당 5발
    [SerializeField] private float barrageSlowMultiplier = 0.5f; // 탄막 중 이동 속도 배율(0.5 = 2배 느림)

    [Header("도탄 (2키)")]
    [SerializeField] private KeyCode ricochetKey = KeyCode.Alpha2;
    [SerializeField] private float ricochetCooldown = 5f;
    [SerializeField] private int ricochetMaxBounces = 3;
    [SerializeField] private float ricochetChainRange = 5f;

    [Header("과부하 (3키)")]
    [SerializeField] private KeyCode overloadKey = KeyCode.Alpha3;
    [SerializeField] private float overloadDuration = 6f;
    [SerializeField] private float overloadRpm = 1440f;
    [SerializeField] private float overloadReloadTime = 0.5f;
    [SerializeField] private float overloadCooldown = 8f;

    [Header("레이 시각화")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float lineDuration = 0.03f;
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private Color missColor = Color.yellow;

    [Header("개발용 화면 표시 (테스트용)")]
    [SerializeField] private bool showDebugHUD = true;

    private int currentAmmo;
    private bool isReloading;
    private float lastShotTime;
    private float lineHideTime;

    private bool barrageActive;
    private float lastBarrageTick;
    private LineRenderer barrageRing;    // 탄막 범위 시각화(링)
    private PlayerController controller; // 탄막 이동 슬로우용

    private bool ricochetActive;
    private float lastRicochetTime = -999f;

    private bool overloadActive;
    private float overloadEndTime;
    private float lastOverloadTime = -999f;

    private Transform Aim => firePoint != null ? firePoint : transform;
    private float CurrentRpm => overloadActive ? overloadRpm : rpm;
    private float FireInterval => 60f / CurrentRpm;

    // UI 노출용
    public int MagSize => magSize;
    public int CurrentAmmo => currentAmmo;
    public bool IsReloading => isReloading;
    public float AmmoRatio => magSize > 0 ? (float)currentAmmo / magSize : 0f;
    public bool IsBarrageActive => barrageActive;
    public bool IsOverloadActive => overloadActive;
    public float RicochetCooldownRemaining => Mathf.Max(0f, lastRicochetTime + ricochetCooldown - Time.time);
    public float OverloadCooldownRemaining => Mathf.Max(0f, lastOverloadTime + overloadCooldown - Time.time);

    private void Awake()
    {
        currentAmmo = magSize;
        controller = GetComponent<PlayerController>();

        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = 0.04f;
        lineRenderer.endWidth = 0.04f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.enabled = false;

        // 탄막 범위 링 (전용 자식 오브젝트)
        GameObject ringObj = new GameObject("BarrageRing");
        ringObj.transform.SetParent(transform, false);
        barrageRing = ringObj.AddComponent<LineRenderer>();
        barrageRing.useWorldSpace = true;
        barrageRing.loop = true;
        barrageRing.positionCount = 48;
        barrageRing.startWidth = 0.05f;
        barrageRing.endWidth = 0.05f;
        barrageRing.material = new Material(Shader.Find("Sprites/Default"));
        barrageRing.startColor = barrageRing.endColor = new Color(1f, 0.5f, 0f, 0.8f); // 주황
        barrageRing.enabled = false;
    }

    // 무기 비활성화 시 슬로우가 남지 않게 원복
    private void OnDisable()
    {
        if (controller != null)
            controller.SpeedMultiplier = 1f;
        barrageActive = false;
    }

    private void Update()
    {
        // 스킬 입력
        if (Input.GetKeyDown(barrageKey))
        {
            barrageActive = !barrageActive;   // 토글
            ApplyBarrageSpeed();              // 이동 속도 조절
        }
        if (Input.GetKeyDown(ricochetKey)) TryRicochet();
        if (Input.GetKeyDown(overloadKey)) TryOverload();

        // 과부하 지속 종료 → 쿨타임 시작
        if (overloadActive && Time.time >= overloadEndTime)
        {
            overloadActive = false;
            lastOverloadTime = Time.time;
        }

        // 좌클릭 홀드 연사
        if (Input.GetMouseButton(0) && CanShoot())
            Shoot();

        // 탄막 틱 (사격과 병행 가능)
        if (barrageActive)
            BarrageTick();

        UpdateBarrageRing();

        // 탄 다 쓰면 자동 재장전
        if (currentAmmo <= 0 && !isReloading)
            StartCoroutine(Reload());

        if (lineRenderer.enabled && Time.time >= lineHideTime)
            lineRenderer.enabled = false;
    }

    // 탄막 켜짐/꺼짐에 따라 플레이어 이동 속도 조절 (PlayerController.SpeedMultiplier 훅)
    private void ApplyBarrageSpeed()
    {
        if (controller != null)
            controller.SpeedMultiplier = barrageActive ? barrageSlowMultiplier : 1f;
    }

    private bool CanShoot()
    {
        return !isReloading && currentAmmo > 0 && Time.time >= lastShotTime + FireInterval;
    }

    private void Shoot()
    {
        lastShotTime = Time.time;
        currentAmmo--;

        Vector2 origin = Aim.position;
        Vector2 dir = Aim.up;
        RaycastHit2D hit = Physics2D.Raycast(origin, dir, attackRange, hitLayers);

        Vector2 endPoint;
        bool didHit = hit.collider != null;
        if (didHit)
        {
            endPoint = hit.point;
            if (hit.collider.TryGetComponent<IDamageable>(out var target) && !target.IsDead)
            {
                target.TakeDamage(attackDamage);

                // 도탄: 첫 명중한 적에서 다른 적에게 튕김
                if (ricochetActive)
                    Ricochet(target, hit.point);
            }
        }
        else
        {
            endPoint = origin + dir * attackRange; // 사정거리 끝 → 선 소멸
        }

        DrawLine(origin, endPoint, didHit);
    }

    // 도탄 연쇄: from 지점에서 가장 가까운 다른 적으로 최대 N번 튕김
    private void Ricochet(IDamageable first, Vector2 fromPoint)
    {
        HashSet<IDamageable> chained = new() { first }; // 적 단위로 중복 방지(콜라이더 여러 개여도 1회)
        Vector2 cur = fromPoint;

        for (int i = 0; i < ricochetMaxBounces; i++)
        {
            IDamageable next = FindNearestEnemy(cur, ricochetChainRange, chained, out Vector2 nextPos);
            if (next == null) break;

            chained.Add(next);
            next.TakeDamage(attackDamage);
            cur = nextPos;
        }
    }

    private IDamageable FindNearestEnemy(Vector2 from, float range, HashSet<IDamageable> exclude, out Vector2 pos)
    {
        Collider2D[] cands = Physics2D.OverlapCircleAll(from, range, hitLayers);
        IDamageable best = null;
        Vector2 bestPos = from;
        float bestDist = float.MaxValue;

        foreach (Collider2D c in cands)
        {
            if (!c.TryGetComponent<IDamageable>(out var d) || d.IsDead) continue;
            if (exclude.Contains(d)) continue;

            float dist = ((Vector2)c.transform.position - from).sqrMagnitude;
            if (dist < bestDist) { bestDist = dist; best = d; bestPos = c.transform.position; }
        }
        pos = bestPos;
        return best;
    }

    // 탄막: 캐릭터 중심 원 범위에 초당 5발로 피해 (탄 소모)
    private void BarrageTick()
    {
        if (isReloading || currentAmmo <= 0) return;
        if (Time.time < lastBarrageTick + 60f / barrageRpm) return;

        lastBarrageTick = Time.time;
        currentAmmo--;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, barrageRadius, hitLayers);
        HashSet<IDamageable> damaged = new(); // 적 단위 중복 방지(콜라이더 여러 개여도 1회)
        foreach (Collider2D h in hits)
            if (h.TryGetComponent<IDamageable>(out var t) && !t.IsDead && damaged.Add(t))
                t.TakeDamage(barrageDamage);
    }

    // 탄막 범위 링 갱신 (켜져 있으면 캐릭터 중심으로 원 표시, 따라다님)
    private void UpdateBarrageRing()
    {
        if (!barrageActive)
        {
            if (barrageRing.enabled) barrageRing.enabled = false;
            return;
        }

        barrageRing.enabled = true;
        Vector2 c = transform.position;
        int seg = barrageRing.positionCount;
        for (int i = 0; i < seg; i++)
        {
            float ang = (float)i / seg * Mathf.PI * 2f;
            barrageRing.SetPosition(i, new Vector3(
                c.x + Mathf.Cos(ang) * barrageRadius,
                c.y + Mathf.Sin(ang) * barrageRadius,
                0f));
        }
    }

    // 도탄 스킬: 즉시 재장전 + 이 탄창은 도탄 탄환. 탄 소진/재장전 시 쿨타임 시작.
    private void TryRicochet()
    {
        if (Time.time < lastRicochetTime + ricochetCooldown)
        {
            Debug.Log($"[기관단총] 도탄 쿨타임 {RicochetCooldownRemaining:F1}초 남음");
            return;
        }

        currentAmmo = magSize;
        isReloading = false;
        ricochetActive = true;
        Debug.Log("[기관단총] 도탄 장전! 이 탄창은 튕김");
    }

    // 과부하: 일정 시간 RPM 2배 + 재장전 단축. 지속 끝난 후 쿨타임.
    private void TryOverload()
    {
        if (overloadActive) return;
        if (Time.time < lastOverloadTime + overloadCooldown)
        {
            Debug.Log($"[기관단총] 과부하 쿨타임 {OverloadCooldownRemaining:F1}초 남음");
            return;
        }

        overloadActive = true;
        overloadEndTime = Time.time + overloadDuration;
        Debug.Log($"[기관단총] 과부하! RPM {overloadRpm}, 재장전 {overloadReloadTime}s ({overloadDuration}초간)");
    }

    private void DrawLine(Vector2 a, Vector2 b, bool didHit)
    {
        Color c = didHit ? hitColor : missColor;
        lineRenderer.startColor = c;
        lineRenderer.endColor = c;
        lineRenderer.SetPosition(0, a);
        lineRenderer.SetPosition(1, b);
        lineRenderer.enabled = true;
        lineHideTime = Time.time + lineDuration;
    }

    private IEnumerator Reload()
    {
        isReloading = true;
        yield return new WaitForSeconds(overloadActive ? overloadReloadTime : reloadTime);
        currentAmmo = magSize;
        isReloading = false;

        // 도탄 탄창이 소진되어 재장전되면 → 도탄 종료 + 쿨타임 시작
        if (ricochetActive)
        {
            ricochetActive = false;
            lastRicochetTime = Time.time;
        }
    }

    private void OnGUI()
    {
        if (!showDebugHUD) return;

        var style = new GUIStyle(GUI.skin.label) { fontSize = 20 };
        style.normal.textColor = Color.white;

        string text =
            $"[기관단총]\n" +
            $"탄약: {currentAmmo} / {magSize}" + (isReloading ? "  (재장전 중...)" : "") + "\n" +
            $"탄막: {(barrageActive ? "ON" : "-")}  |  " +
            $"과부하: {(overloadActive ? "ON" : "-")}  |  " +
            $"도탄: {(ricochetActive ? "장전" : "-")}\n" +
            $"도탄쿨: {RicochetCooldownRemaining:F1}s  |  과부하쿨: {OverloadCooldownRemaining:F1}s";

        GUI.Label(new Rect(14, 12, 600, 140), text, style);
    }
}
