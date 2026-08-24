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

    public static void FireRadial(GameObject prefab, Vector2 center, Vector2 baseDir, int count, float speed, float damage)
    {
        if (prefab == null || count <= 0) return;

        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x);
        float step = Mathf.PI * 2f / count;

        for (int i = 0; i < count; i++)
        {
            float angle = baseAngle + step * i;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            GameObject obj = Instantiate(prefab, center, Quaternion.identity);
            obj.GetComponent<EnemyProjectile>().Init(dir, speed, damage);
        }
    }
}
