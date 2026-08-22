using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("이동")]
    [SerializeField] private float moveSpeed = 5f;

    /// <summary>이동 속도 배율. 외부(스킬 등)에서 조절 가능 (기본값 1)</summary>
    public float SpeedMultiplier = 1f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Camera mainCamera;

    public float MoveSpeed => moveSpeed;
    public Vector2 MoveDirection => moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
    }

    private void Update()
    {
        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        RotateTowardMouse();
    }

    private void FixedUpdate()
    {
        rb.velocity = moveInput * moveSpeed * SpeedMultiplier;
    }

    // 마우스 방향으로 플레이어 회전 (탑뷰)
    private void RotateTowardMouse()
    {
        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = (mouseWorld - transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
