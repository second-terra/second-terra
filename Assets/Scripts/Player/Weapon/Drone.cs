using System.Collections;
using UnityEngine;

// 드론 본체 (DroneWeapon이 런타임 생성).
// 별도 체력 / 커서 위치로 이동 / 커서 방향 5점사(히트스캔) / 적 충돌 시 피해.
// 감전 상태면 충돌 시 드론은 피해 없고 적이 피해를 받는다.
[RequireComponent(typeof(Rigidbody2D))]
public class Drone : MonoBehaviour
{
    private float maxHp;
    private float currentHp;
    private float moveSpeed;
    private LayerMask hitLayers;
    private float shotDamage;
    private float shotRange;
    private float contactDamageToDrone;      // 적 충돌 시 드론이 받는 피해
    private float electrifiedDamageToEnemy;   // 감전 시 충돌한 적이 받는 피해

    private const float ContactInterval = 0.5f; // 충돌 피해 간격(매 프레임 방지)
    private float lastContactTime = -99f;
    private float lastShotLineHide;

    private bool electrified;
    private float electrifiedUntil;

    private Vector2 targetPos;
    private LineRenderer body;      // 드론 몸통(작은 원 링)
    private LineRenderer shotLine;  // 사격 선

    public bool IsAlive => currentHp > 0f;
    public float MaxHp => maxHp;
    public float CurrentHp => currentHp;
    public float HpRatio => maxHp > 0f ? currentHp / maxHp : 0f;
    public bool IsElectrified => electrified;

    public void Init(float maxHp, float moveSpeed, LayerMask hitLayers,
                     float shotDamage, float shotRange, float contactDamageToDrone, float electrifiedDamageToEnemy)
    {
        this.maxHp = maxHp;
        currentHp = maxHp;
        this.moveSpeed = moveSpeed;
        this.hitLayers = hitLayers;
        this.shotDamage = shotDamage;
        this.shotRange = shotRange;
        this.contactDamageToDrone = contactDamageToDrone;
        this.electrifiedDamageToEnemy = electrifiedDamageToEnemy;
        targetPos = transform.position;

        SetupComponents();
    }

    private void SetupComponents()
    {
        var rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        var col = GetComponent<CircleCollider2D>();
        if (col == null) col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.3f;

        // 몸통 링 (로컬 좌표라 드론 따라 자동 이동)
        body = gameObject.AddComponent<LineRenderer>();
        body.useWorldSpace = false;
        body.loop = true;
        body.positionCount = 24;
        body.startWidth = body.endWidth = 0.08f;
        body.material = new Material(Shader.Find("Sprites/Default"));
        body.sortingOrder = 100;
        for (int i = 0; i < 24; i++)
        {
            float a = (float)i / 24 * Mathf.PI * 2f;
            body.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.3f, Mathf.Sin(a) * 0.3f, 0f));
        }
        UpdateBodyColor();

        // 사격 선 (월드 좌표)
        var lineObj = new GameObject("DroneShotLine");   // 자식 아님(독립 오브젝트)
        shotLine = lineObj.AddComponent<LineRenderer>();
        shotLine.useWorldSpace = true;    // 월드 좌표 (기관단총 사격선과 동일, 검증됨)
        shotLine.positionCount = 2;
        shotLine.startWidth = shotLine.endWidth = 0.08f;
        shotLine.material = new Material(Shader.Find("Sprites/Default"));
        shotLine.startColor = shotLine.endColor = Color.cyan;
        shotLine.sortingOrder = 100;
        shotLine.enabled = false;
    }

    private void UpdateBodyColor()
    {
        Color c = electrified ? new Color(1f, 1f, 0.2f) : new Color(0.4f, 0.8f, 1f); // 감전=노랑 / 평소=하늘
        if (body != null) body.startColor = body.endColor = c;
    }

    private void Update()
    {
        if (body == null) return; // Init 전이면 대기

        // 커서로 지정된 목표로 이동
        transform.position = Vector2.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

        // 감전 만료
        if (electrified && Time.time >= electrifiedUntil)
        {
            electrified = false;
            UpdateBodyColor();
        }

        // 사격 선 숨기기
        if (shotLine != null && shotLine.enabled && Time.time >= lastShotLineHide)
            shotLine.enabled = false;
    }

    // 우클릭: 이 위치로 이동
    public void MoveTo(Vector2 pos) => targetPos = pos;

    // 평타: 커서 방향으로 N점사 (히트스캔)
    public void FireBurst(Vector2 dir, int count, float totalTime)
    {
        StartCoroutine(BurstRoutine(dir, count, totalTime));
    }

    private IEnumerator BurstRoutine(Vector2 dir, int count, float totalTime)
    {
        float gap = count > 1 ? totalTime / (count - 1) : 0f;
        for (int i = 0; i < count; i++)
        {
            FireOne(dir);
            if (gap > 0f) yield return new WaitForSeconds(gap);
        }
    }

    private void FireOne(Vector2 dir)
    {
        Vector2 origin = transform.position;
        Vector2 n = dir.normalized;
        Vector2 rayStart = origin + n * 0.4f; // 드론 자기 콜라이더(반경 0.3)를 안 맞게 앞에서 시작
        RaycastHit2D hit = Physics2D.Raycast(rayStart, n, shotRange, hitLayers);

        Vector2 end;
        if (hit.collider != null)
        {
            end = hit.point;
            if (hit.collider.TryGetComponent<IDamageable>(out var t) && !t.IsDead)
                t.TakeDamage(shotDamage);
        }
        else
        {
            end = rayStart + n * shotRange;
        }

        if (shotLine != null)
        {
            // 월드 좌표: 드론 → 목표
            shotLine.SetPosition(0, origin);
            shotLine.SetPosition(1, end);
            shotLine.enabled = true;
            lastShotLineHide = Time.time + 0.12f;
        }
    }

    // 감전 켜기 (지속시간)
    public void SetElectrified(float duration)
    {
        electrified = true;
        electrifiedUntil = Time.time + duration;
        UpdateBodyColor();
    }

    // 적과 충돌: 감전이면 적이 피해, 아니면 드론이 피해
    private void OnTriggerStay2D(Collider2D other)
    {
        if (Time.time < lastContactTime + ContactInterval) return;
        if (!other.TryGetComponent<IDamageable>(out var enemy) || enemy.IsDead) return;

        lastContactTime = Time.time;

        if (electrified)
        {
            enemy.TakeDamage(electrifiedDamageToEnemy);
        }
        else
        {
            currentHp = Mathf.Max(0f, currentHp - contactDamageToDrone);
            if (currentHp <= 0f)
                gameObject.SetActive(false); // 죽음 (DroneWeapon이 감지해서 재구성 가능)
        }
    }
}
