using System.Collections.Generic;
using UnityEngine;

// 드론 스웜용 초소형 드론. 가장 가까운 적에게 날아가 자폭(원형 피해).
// 타겟이 없거나 수명이 다하면 그 자리에서 자폭.
public class SwarmDrone : MonoBehaviour
{
    private Transform target;
    private float speed;
    private float damage;
    private float explodeRadius;
    private LayerMask hitLayers;
    private float life;
    private bool exploded;

    public void Init(Vector2 startPos, Transform target, float speed, float damage,
                     float explodeRadius, LayerMask hitLayers, float life)
    {
        transform.position = startPos;
        this.target = target;
        this.speed = speed;
        this.damage = damage;
        this.explodeRadius = explodeRadius;
        this.hitLayers = hitLayers;
        this.life = life;

        // 작은 링 비주얼 (주황)
        var lr = gameObject.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.positionCount = 12;
        lr.startWidth = lr.endWidth = 0.05f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = new Color(1f, 0.5f, 0.1f);
        lr.sortingOrder = 100;
        for (int i = 0; i < 12; i++)
        {
            float a = (float)i / 12 * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.15f, Mathf.Sin(a) * 0.15f, 0f));
        }
    }

    private void Update()
    {
        life -= Time.deltaTime;

        if (life <= 0f || target == null || target.gameObject == null)
        {
            Explode();
            return;
        }

        transform.position = Vector2.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

        if (Vector2.Distance(transform.position, target.position) < 0.4f)
            Explode();
    }

    private void Explode()
    {
        if (exploded) return;
        exploded = true;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explodeRadius, hitLayers);
        HashSet<IDamageable> damaged = new();
        foreach (Collider2D h in hits)
            if (h.TryGetComponent<IDamageable>(out var d) && !d.IsDead && damaged.Add(d))
                d.TakeDamage(damage);

        Destroy(gameObject);
    }
}
