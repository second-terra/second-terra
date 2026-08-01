using System;
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
    public event Action<float, float> OnHealthChanged;

    /// <summary>피해를 가로챌 수 있는 훅. true 반환 시 피해 무시 (예: 패링)</summary>
    public Func<float, bool> DamageInterceptor;

    private void Awake()
    {
        currentHp = maxHp;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;
        if (DamageInterceptor != null && DamageInterceptor(amount)) return;

        currentHp = Mathf.Max(0f, currentHp - amount);
        OnHealthChanged?.Invoke(currentHp, maxHp);

        if (IsDead)
        {
            onDeath?.Invoke();
            Destroy(gameObject);
        }
    }

    public void Heal(float amount)
    {
        if (IsDead) return;

        currentHp = Mathf.Min(maxHp, currentHp + amount);
        OnHealthChanged?.Invoke(currentHp, maxHp);
    }
}
