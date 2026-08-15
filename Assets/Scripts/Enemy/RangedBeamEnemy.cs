using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class RangedBeamEnemy : RangedEnemyBase
{
    private enum BeamState { Idle, Telegraph, Fire, Cooldown }

    [Header("빔 텔레그래프")]
    [SerializeField] private float telegraphDuration = 0.8f;
    [SerializeField] private AudioClip warningClip;
    [SerializeField] private Color telegraphColor = new Color(1f, 0f, 0f, 0.25f);

    [Header("빔 발사")]
    [SerializeField] private float beamRange = 10f;
    [SerializeField] private float beamOriginOffset = 0.6f;
    [SerializeField] private float beamVisibleDuration = 0.15f;
    [SerializeField] private AudioClip fireClip;
    [SerializeField] private Color fireColor = Color.red;

    [Header("표시")]
    [SerializeField] private float lineWidth = 0.08f;

    private BeamState state = BeamState.Idle;
    private float stateTimer;
    private Vector2 lockedDir;

    private LineRenderer line;
    private AudioSource audioSource;

    protected override void Awake()
    {
        base.Awake();
        audioSource = GetComponent<AudioSource>();

        line = gameObject.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.widthMultiplier = lineWidth;
        line.enabled = false;
    }

    protected override void Update()
    {
        if (isDead || !HasPlayer) return;

        switch (state)
        {
            case BeamState.Idle:     UpdateIdle();     break;
            case BeamState.Telegraph: UpdateTelegraph(); break;
            case BeamState.Fire:      UpdateFire();      break;
            case BeamState.Cooldown:  UpdateCooldown();  break;
        }
    }

    private void UpdateIdle()
    {
        FaceTarget();
        if (DistToPlayer() <= detectionRange)
            EnterTelegraph();
    }

    private void EnterTelegraph()
    {
        state = BeamState.Telegraph;
        stateTimer = telegraphDuration;
        lockedDir = DirToPlayer();

        ShowLine(telegraphColor, lockedDir * beamRange);
        if (warningClip != null)
            audioSource.PlayOneShot(warningClip);
    }

    private void UpdateTelegraph()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
            EnterFire();
    }

    private void EnterFire()
    {
        state = BeamState.Fire;
        stateTimer = beamVisibleDuration;

        Vector2 origin = (Vector2)transform.position + lockedDir * beamOriginOffset;
        var hit = Physics2D.Raycast(origin, lockedDir, beamRange);

        Vector2 endPoint = hit.collider != null ? hit.point : origin + lockedDir * beamRange;
        if (hit.collider != null && hit.collider.TryGetComponent<PlayerStats>(out var player))
            player.TakeDamage(EnemyBalance.RangedBeamDamage);

        ShowLine(fireColor, endPoint - (Vector2)transform.position);
        if (fireClip != null)
            audioSource.PlayOneShot(fireClip);
    }

    private void UpdateFire()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
            EnterCooldown();
    }

    private void EnterCooldown()
    {
        line.enabled = false;
        state = BeamState.Cooldown;
        stateTimer = attackCooldown;
    }

    private void UpdateCooldown()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
            state = BeamState.Idle;
    }

    private void ShowLine(Color color, Vector2 localEnd)
    {
        line.enabled = true;
        line.startColor = color;
        line.endColor = color;
        line.SetPosition(0, transform.position);
        line.SetPosition(1, transform.position + (Vector3)localEnd);
    }

    protected override void OnDied()
    {
        line.enabled = false;
        base.OnDied();
    }

    // 발사는 EnterFire()의 자체 히트스캔 타이밍으로 처리되며, 기본 쿨다운 루프(PerformAttack)는 사용하지 않는다.
    protected override void PerformAttack(IDamageable target) { }
}
