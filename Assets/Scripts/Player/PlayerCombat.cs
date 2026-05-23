using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("투사체")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float attackCooldown = 0.3f;

    [Header("스탯")]
    [SerializeField] private float attackDamage = 10f;

    private float lastAttackTime;

    private void Update()
    {
        if (Input.GetMouseButton(0) && CanAttack())
            Shoot();
    }

    private bool CanAttack()
    {
        return Time.time >= lastAttackTime + attackCooldown;
    }

    private void Shoot()
    {
        lastAttackTime = Time.time;

        GameObject proj = ProjectilePool.Instance.Get();
        proj.transform.position = firePoint.position;
        proj.transform.rotation = firePoint.rotation;
        proj.GetComponent<Projectile>().Init(attackDamage);
    }
}
