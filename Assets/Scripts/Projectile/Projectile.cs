using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 3f;

    private float damage;
    private float spawnTime;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Init(float damage)
    {
        this.damage = damage;
        spawnTime = Time.time;
        rb.velocity = transform.up * speed;
    }

    private void Update()
    {
        if (Time.time >= spawnTime + lifetime)
            ReturnToPool();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<IDamageable>(out var target))
        {
            target.TakeDamage(damage);
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        rb.velocity = Vector2.zero;
        ProjectilePool.Instance.Return(gameObject);
    }
}
