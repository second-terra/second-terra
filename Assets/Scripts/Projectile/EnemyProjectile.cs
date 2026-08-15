using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private float lifetime = 4f;

    private float damage;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Init(Vector2 direction, float speed, float damage)
    {
        this.damage = damage;
        rb.velocity = direction * speed;
        Destroy(gameObject, lifetime);
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
