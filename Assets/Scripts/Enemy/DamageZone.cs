using System.Collections;
using UnityEngine;

public class DamageZone : MonoBehaviour
{
    private const float ImpactVisibleDuration = 0.2f;

    private static Sprite cachedCircleSprite;

    private float radius;
    private float damage;
    private SpriteRenderer sr;

    public void Init(float radius, float telegraphDuration, float damage)
    {
        this.radius = radius;
        this.damage = damage;

        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = GetCircleSprite();
        sr.color = new Color(1f, 0.2f, 0.2f, 0.35f);
        transform.localScale = Vector3.one * radius * 2f;

        StartCoroutine(Sequence(telegraphDuration));
    }

    private IEnumerator Sequence(float telegraphDuration)
    {
        yield return new WaitForSeconds(telegraphDuration);
        Detonate();
        yield return new WaitForSeconds(ImpactVisibleDuration);
        Destroy(gameObject);
    }

    // OverlapCircle은 겹친 콜라이더 중 임의의 하나만 반환하므로, 플레이어가 다른 콜라이더와
    // 함께 겹쳐있을 때 플레이어를 놓칠 수 있음 -> 겹친 것 전부를 훑어서 플레이어를 찾는다.
    private void Detonate()
    {
        sr.color = new Color(1f, 0.4f, 0f, 0.6f);

        var hits = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<PlayerStats>(out var player))
            {
                player.TakeDamage(damage);
                return;
            }
        }
    }

    private static Sprite GetCircleSprite()
    {
        if (cachedCircleSprite != null) return cachedCircleSprite;

        const int size = 64;
        var tex = new Texture2D(size, size);
        var pixels = new Color[size * size];
        float r = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(r, r));
                pixels[y * size + x] = dist <= r ? Color.white : Color.clear;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        tex.name = "DamageZoneCircleTex";

        cachedCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        cachedCircleSprite.name = "DamageZoneCircleSprite";
        return cachedCircleSprite;
    }
}
