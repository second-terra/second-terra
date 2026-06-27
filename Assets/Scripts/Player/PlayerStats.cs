using UnityEngine;
using UnityEngine.Events;

public class PlayerStats : MonoBehaviour, IDamageable
{
    [Header("HP")]
    [SerializeField] private float maxHp = 100f;
    private float currentHp;

    public float MaxHp => maxHp;
    public float CurrentHp => currentHp;
    public bool IsDead => currentHp <= 0f;

    public UnityEvent onDeath;

    private void Awake()
    {
        currentHp = maxHp;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;

        currentHp = Mathf.Max(0f, currentHp - amount);

        if (IsDead)
            onDeath?.Invoke();
    }

    public void Heal(float amount)
    {
        if (IsDead) return;

        currentHp = Mathf.Min(maxHp, currentHp + amount);
    }
}
