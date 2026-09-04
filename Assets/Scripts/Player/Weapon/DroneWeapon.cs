using System.Collections.Generic;
using UnityEngine;

// 드론 무기(의체) 컨트롤러. 드론 본체를 런타임 생성·관리.
// 좌클릭: 커서 방향 5점사 / 우클릭: 커서 위치로 드론 이동.
// 스킬: 1 드론스웜, 2 음파(둔화), 3 감전, 4 폭파+재구성.
public class DroneWeapon : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private LayerMask hitLayers;    // 적 레이어

    [Header("드론 본체")]
    [SerializeField] private float droneHp = 50f;
    [SerializeField] private float droneMoveSpeed = 8f;
    [SerializeField] private float droneContactDamage = 5f;   // 적 충돌 시 드론이 받는 피해
    [SerializeField] private Vector2 droneSpawnOffset = new Vector2(1f, 1f);

    [Header("평타 (좌클릭, 5점사)")]
    [SerializeField] private float shotDamage = 4f;
    [SerializeField] private float shotRange = 12f;
    [SerializeField] private int burstCount = 5;
    [SerializeField] private float burstTime = 0.5f;     // 5점사 완료 시간
    [SerializeField] private float betweenBurst = 0.3f;  // 다음 사격까지 딜레이

    [Header("감전 (3키)")]
    [SerializeField] private KeyCode electrifyKey = KeyCode.Alpha3;
    [SerializeField] private float electrifyDuration = 5f;
    [SerializeField] private float electrifyCooldown = 6f;
    [SerializeField] private float electrifiedDamageToEnemy = 8f;

    [Header("드론 스웜 (1키)")]
    [SerializeField] private KeyCode swarmKey = KeyCode.Alpha1;
    [SerializeField] private int swarmCount = 6;
    [SerializeField] private float swarmSpeed = 10f;
    [SerializeField] private float swarmDamage = 10f;
    [SerializeField] private float swarmExplodeRadius = 1.2f;
    [SerializeField] private float swarmLife = 3f;
    [SerializeField] private float swarmCooldown = 4f;

    [Header("음파 (2키)")]
    [SerializeField] private KeyCode sonicKey = KeyCode.Alpha2;
    [SerializeField] private float sonicRange = 5f;
    [SerializeField] private float sonicHalfAngle = 45f;
    [SerializeField] private float sonicCooldown = 7f;
    // EnemyBase.ApplySlow는 인자가 허용 범위를 벗어나면 조용히 무시한다.
    // 그러면 둔화가 안 걸렸는데 HUD에는 적중으로 표시되므로, 인스펙터에서 아예 못 넣도록 막아둔다.
    [Range(0f, 1f)]
    [SerializeField] private float sonicSlowMoveMultiplier = 0.5f;   // 이동속도 배율 (0.5 = 절반)
    [Range(0.01f, 1f)]
    [SerializeField] private float sonicSlowAttackMultiplier = 0.7f; // 공격속도 배율 (0.7 = 30% 느려짐)
    [Min(0.01f)]
    [SerializeField] private float sonicSlowDuration = 3f;           // 둔화 지속시간(초), 0 이하면 둔화가 무시됨

    [Header("폭파+재구성 (4키)")]
    [SerializeField] private KeyCode reconstructKey = KeyCode.Alpha4;
    [SerializeField] private float reconstructDamage = 40f;
    [SerializeField] private float reconstructRadius = 2.5f;
    [SerializeField] private float reconstructCooldown = 10f;
    [SerializeField] private float reconstructElectrifyDuration = 5f; // 새 드론 기본 감전 시간

    [Header("개발용 HUD")]
    [SerializeField] private bool showDebugHUD = true;

    private Drone drone;
    private float lastBurstTime = -99f;
    private float lastElectrifyTime = -999f;
    private float lastSwarmTime = -999f;
    private float lastSonicTime = -999f;
    private int lastSonicHitCount;   // 개발용 HUD 표시: 마지막 음파로 둔화시킨 적 수
    private float lastReconstructTime = -999f;
    private Camera cam;

    private void Awake()
    {
        cam = Camera.main;
    }

    // 드론은 이 무기가 활성화되어 있는 동안에만 존재한다.
    // Awake에서 스폰하면 무기 교체로 컴포넌트를 꺼도 드론이 월드에 남아 계속 동작하기 때문.
    private void OnEnable()
    {
        if (drone == null)
            SpawnDrone();
    }

    private void OnDisable()
    {
        if (drone != null)
        {
            Destroy(drone.gameObject);
            drone = null;
        }
    }

    private void SpawnDrone()
    {
        var go = new GameObject("Drone");
        go.transform.position = (Vector2)transform.position + droneSpawnOffset;
        drone = go.AddComponent<Drone>();
        drone.Init(droneHp, droneMoveSpeed, hitLayers, shotDamage, shotRange, droneContactDamage, electrifiedDamageToEnemy);
    }

    private void Update()
    {
        bool droneReady = drone != null && drone.IsAlive;
        Vector2 mouseWorld = cam != null ? (Vector2)cam.ScreenToWorldPoint(Input.mousePosition) : (Vector2)transform.position;

        // 우클릭: 드론을 커서 위치로 이동
        if (droneReady && Input.GetMouseButton(1))
            drone.MoveTo(mouseWorld);

        // 좌클릭: 평타 5점사 (사격 완료 + 다음 딜레이 지난 뒤 재발사)
        if (droneReady && Input.GetMouseButton(0) && Time.time >= lastBurstTime + burstTime + betweenBurst)
        {
            Vector2 dir = (mouseWorld - (Vector2)drone.transform.position).normalized;
            if (dir == Vector2.zero) dir = Vector2.up;
            drone.FireBurst(dir, burstCount, burstTime);
            lastBurstTime = Time.time;
        }

        // 스킬
        if (Input.GetKeyDown(swarmKey)) TrySwarm();          // 1 드론 스웜
        if (Input.GetKeyDown(sonicKey)) TrySonic();          // 2 음파
        if (Input.GetKeyDown(electrifyKey)) TryElectrify();  // 3 감전
        if (Input.GetKeyDown(reconstructKey)) TryReconstruct(); // 4 폭파+재구성
    }

    private void TryElectrify()
    {
        if (drone == null || !drone.IsAlive) return;
        if (Time.time < lastElectrifyTime + electrifyCooldown)
            return;
        lastElectrifyTime = Time.time;
        drone.SetElectrified(electrifyDuration);
    }

    // 드론 스웜: 초소형 드론 N개 사출 → 각자 가장 가까운 적에게 날아가 자폭
    private void TrySwarm()
    {
        if (Time.time < lastSwarmTime + swarmCooldown)
            return;
        lastSwarmTime = Time.time;

        for (int i = 0; i < swarmCount; i++)
        {
            Transform tgt = FindNearestEnemy(transform.position);
            var go = new GameObject("SwarmDrone");
            var sd = go.AddComponent<SwarmDrone>();
            Vector2 spawn = (Vector2)transform.position + Random.insideUnitCircle * 0.5f;
            sd.Init(spawn, tgt, swarmSpeed, swarmDamage, swarmExplodeRadius, hitLayers, swarmLife);
        }
    }

    // 음파: 드론 전방(커서 방향) 부채꼴 내 적을 일정 시간 둔화(이속·공속).
    private void TrySonic()
    {
        if (drone == null || !drone.IsAlive) return;
        if (Time.time < lastSonicTime + sonicCooldown)
            return;
        lastSonicTime = Time.time;
        lastSonicHitCount = 0;

        Vector2 origin = drone.transform.position;
        Vector2 mouseWorld = cam != null ? (Vector2)cam.ScreenToWorldPoint(Input.mousePosition) : origin + Vector2.up;
        Vector2 fwd = (mouseWorld - origin).normalized;
        if (fwd == Vector2.zero) fwd = Vector2.up;

        foreach (var e in EnemyBase.ActiveEnemies)
        {
            if (e == null || e.IsDead) continue;
            Vector2 to = (Vector2)e.transform.position - origin;
            if (to.magnitude > sonicRange) continue;
            if (Vector2.Angle(fwd, to) > sonicHalfAngle) continue;

            e.ApplySlow(sonicSlowMoveMultiplier, sonicSlowAttackMultiplier, sonicSlowDuration);
            lastSonicHitCount++;
        }
    }

    // 폭파+재구성: 드론 있으면 자폭(원형 피해) 후 의체 위치에 재소환. 드론 없으면 재소환만.
    // 새 드론은 일정 시간 감전 활성. 드론이 죽어있으면 쿨타임 무시.
    private void TryReconstruct()
    {
        bool droneDead = drone == null || !drone.IsAlive;
        if (!droneDead && Time.time < lastReconstructTime + reconstructCooldown)
            return;
        lastReconstructTime = Time.time;

        // 살아있는 드론이면 자폭 원형 피해
        if (drone != null && drone.IsAlive)
        {
            Vector2 pos = drone.transform.position;
            Collider2D[] hits = Physics2D.OverlapCircleAll(pos, reconstructRadius, hitLayers);
            HashSet<IDamageable> damaged = new();
            foreach (Collider2D h in hits)
                if (h.TryGetComponent<IDamageable>(out var d) && !d.IsDead && damaged.Add(d))
                    d.TakeDamage(reconstructDamage);
        }
        if (drone != null) Destroy(drone.gameObject);

        // 재소환 + 기본 감전
        SpawnDrone();
        drone.SetElectrified(reconstructElectrifyDuration);
    }

    private Transform FindNearestEnemy(Vector2 from)
    {
        Transform best = null;
        float bestDist = float.MaxValue;
        foreach (var e in EnemyBase.ActiveEnemies)
        {
            if (e == null || e.IsDead) continue;
            float d = ((Vector2)e.transform.position - from).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = e.transform; }
        }
        return best;
    }

    // ===== UI 노출용 (김세원님) =====
    public bool HasDrone => drone != null && drone.IsAlive;
    public float DroneHpRatio => drone != null ? drone.HpRatio : 0f;
    public bool IsElectrified => drone != null && drone.IsElectrified;
    public float ElectrifyCooldownRemaining => Mathf.Max(0f, lastElectrifyTime + electrifyCooldown - Time.time);

    private void OnGUI()
    {
        if (!showDebugHUD) return;
        var style = new GUIStyle(GUI.skin.label) { fontSize = 20 };
        style.normal.textColor = Color.white;

        string hp = HasDrone ? $"{drone.CurrentHp:F0}/{drone.MaxHp:F0}" : "없음(죽음, 4로 재구성)";
        float swarmCd = Mathf.Max(0f, lastSwarmTime + swarmCooldown - Time.time);
        float sonicCd = Mathf.Max(0f, lastSonicTime + sonicCooldown - Time.time);
        float reconCd = Mathf.Max(0f, lastReconstructTime + reconstructCooldown - Time.time);
        GUI.Label(new Rect(14, 12, 460, 180),
            $"[드론]\n드론 HP: {hp}\n" +
            $"감전: {(IsElectrified ? "ON" : "-")}\n" +
            $"1 스웜쿨 {swarmCd:F1}s | 2 음파쿨 {sonicCd:F1}s (직전 둔화 {lastSonicHitCount}체)\n" +
            $"3 감전쿨 {ElectrifyCooldownRemaining:F1}s | 4 재구성쿨 {reconCd:F1}s", style);
    }
}
