using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class BossBase : EnemyBase
{
    [Header("보스 - 패턴 진행")]
    [SerializeField] private float baseInterval = 2f;

    protected class BossPattern
    {
        public string Name;
        public float Weight;
        public Func<bool> CanUse = () => true;
        public Func<IEnumerator> Execute;
    }

    protected Rigidbody2D rb;

    private List<BossPattern> patterns;
    private BossPattern lastPattern;
    private bool isBusy;
    private float nextPatternTime;
    private Coroutine patternCoroutine;

    protected abstract List<BossPattern> BuildPatterns();

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
    }

    protected override void Start()
    {
        base.Start();
        patterns = BuildPatterns();
        nextPatternTime = Time.time + baseInterval * GetIntervalScale();
    }

    protected virtual void Update()
    {
        if (isDead || isBusy || !HasPlayer) return;
        if (Time.time < nextPatternTime) return;

        var picked = PickPattern();
        if (picked == null) return;

        patternCoroutine = StartCoroutine(RunPattern(picked));
    }

    private BossPattern PickPattern()
    {
        var candidates = new List<BossPattern>();
        foreach (var p in patterns)
        {
            if (!p.CanUse()) continue;
            if (p == lastPattern && patterns.Count > 1) continue;
            candidates.Add(p);
        }

        if (candidates.Count == 0)
        {
            foreach (var p in patterns)
                if (p.CanUse()) candidates.Add(p);
        }

        if (candidates.Count == 0) return null;

        float totalWeight = 0f;
        foreach (var p in candidates) totalWeight += p.Weight;

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;
        foreach (var p in candidates)
        {
            cumulative += p.Weight;
            if (roll <= cumulative) return p;
        }

        return candidates[candidates.Count - 1];
    }

    private IEnumerator RunPattern(BossPattern pattern)
    {
        isBusy = true;
        // 패턴 코루틴 안에서 예외가 나도 isBusy가 영원히 true로 남아 보스가 멈추지 않도록 finally로 보장.
        try
        {
            yield return StartCoroutine(pattern.Execute());
        }
        finally
        {
            isBusy = false;
            lastPattern = pattern;
            nextPatternTime = Time.time + baseInterval * GetIntervalScale();
        }
    }

    protected virtual float GetIntervalScale() => 1f;

    protected float TempoScale => GetIntervalScale();

    protected override void PerformAttack(IDamageable target) { }

    // 사망 시 진행 중이던 패턴 코루틴을 멈춰서, 죽은 뒤에도 공격이 계속 나가는 것을 방지.
    protected override void OnDied()
    {
        if (patternCoroutine != null)
        {
            StopCoroutine(patternCoroutine);
            patternCoroutine = null;
        }
        isBusy = false;

        base.OnDied();
    }
}
