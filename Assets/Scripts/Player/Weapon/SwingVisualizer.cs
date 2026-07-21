using UnityEngine;

// 공격 히트박스를 플레이 중에 잠깐 보여주는 개발용 시각화 (원 링).
// 아트 에셋 없이 LineRenderer로 그림. 나중에 진짜 이펙트로 교체 예정.
public class SwingVisualizer : MonoBehaviour
{
    [SerializeField] private int segments = 40;      // 원을 몇 조각으로 그릴지 (많을수록 매끈)
    [SerializeField] private float lineWidth = 0.06f;
    [SerializeField] private float showDuration = 0.15f; // 표시 시간

    private LineRenderer lr;
    private float hideTime;

    private void Awake()
    {
        // 전용 자식 오브젝트에 LineRenderer 생성 (플레이어 본체와 분리)
        GameObject go = new GameObject("SwingRing");
        go.transform.SetParent(transform, false);

        lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.positionCount = segments;
        lr.enabled = false;
    }

    private void Update()
    {
        if (lr.enabled && Time.time >= hideTime)
            lr.enabled = false;
    }

    // center를 중심으로 radius 원을 color 색으로 잠깐 표시
    public void FlashCircle(Vector2 center, float radius, Color color)
    {
        lr.startColor = color;
        lr.endColor = color;
        lr.positionCount = segments;

        for (int i = 0; i < segments; i++)
        {
            float ang = (float)i / segments * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(
                center.x + Mathf.Cos(ang) * radius,
                center.y + Mathf.Sin(ang) * radius,
                0f));
        }

        lr.enabled = true;
        hideTime = Time.time + showDuration;
    }

    // center 기준 size(폭,길이) 사각형을 angleDeg 회전시켜 color 색으로 잠깐 표시
    public void FlashBox(Vector2 center, Vector2 size, float angleDeg, Color color)
    {
        lr.startColor = color;
        lr.endColor = color;
        lr.positionCount = 4;

        Quaternion rot = Quaternion.Euler(0f, 0f, angleDeg);
        Vector2 h = size * 0.5f;
        Vector2[] local =
        {
            new Vector2(-h.x, -h.y),
            new Vector2( h.x, -h.y),
            new Vector2( h.x,  h.y),
            new Vector2(-h.x,  h.y),
        };
        for (int i = 0; i < 4; i++)
            lr.SetPosition(i, (Vector3)center + rot * (Vector3)local[i]);

        lr.enabled = true;
        hideTime = Time.time + showDuration;
    }
}
