using UnityEngine;

// 3섹터 보스 "분출-직사" 전용 초고속 투사체. 날아가는 궤적을 따라 바닥에 즉발 피해 장판을 남긴다.
[RequireComponent(typeof(Rigidbody2D))]
public class TrailProjectile : MonoBehaviour
{
    [SerializeField] private float lifetime = 2f;
    [SerializeField] private float trailInterval = 0.08f;
    [SerializeField] private float trailZoneRadius = 0.6f;
    [SerializeField] private float trailZoneDamage = 6f;

    private float damage;
    private Rigidbody2D rb;
    private float lastTrailTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Init(Vector2 direction, float speed, float damage)
    {
        this.damage = damage;
        rb.velocity = direction * speed;
        lastTrailTime = Time.time;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (Time.time - lastTrailTime < trailInterval) return;
        lastTrailTime = Time.time;
        DropTrailZone();
    }

    private void DropTrailZone()
    {
        var zoneGo = new GameObject("TrailZone");
        zoneGo.transform.position = transform.position;
        zoneGo.AddComponent<DamageZone>().Init(trailZoneRadius, 0f, trailZoneDamage);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<PlayerStats>(out var player))
        {
            player.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
