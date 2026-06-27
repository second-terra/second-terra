using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("히트스캔")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private float attackCooldown = 0.3f;
    [SerializeField] private float attackRange = 20f;
    [SerializeField] private LayerMask hitLayers;

    [Header("스탯")]
    [SerializeField] private float attackDamage = 10f;

    [Header("레이 시각화")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float lineDuration = 0.05f;
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private Color missColor = Color.yellow;

    private float lastAttackTime;
    private float lineHideTime;

    private void Awake()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.enabled = false;
    }

    private void Update()
    {
        if (Input.GetMouseButton(0) && CanAttack())
            Shoot();

        if (lineRenderer.enabled && Time.time >= lineHideTime)
            lineRenderer.enabled = false;
    }

    private bool CanAttack()
    {
        return Time.time >= lastAttackTime + attackCooldown;
    }

    private void Shoot()
    {
        lastAttackTime = Time.time;

        Vector2 origin = firePoint.position;
        Vector2 direction = firePoint.up;
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, attackRange, hitLayers);

        Vector2 endPoint;
        if (hit.collider != null)
        {
            endPoint = hit.point;
            lineRenderer.startColor = hitColor;
            lineRenderer.endColor = hitColor;

            if (hit.collider.TryGetComponent<IDamageable>(out var target))
                target.TakeDamage(attackDamage);
        }
        else
        {
            endPoint = origin + direction * attackRange;
            lineRenderer.startColor = missColor;
            lineRenderer.endColor = missColor;
        }

        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, endPoint);
        lineRenderer.enabled = true;
        lineHideTime = Time.time + lineDuration;
    }
}
