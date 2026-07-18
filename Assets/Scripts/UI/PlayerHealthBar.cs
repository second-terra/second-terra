using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBar : MonoBehaviour
{
    [SerializeField] private Slider hpSlider;
    [SerializeField] private PlayerStats playerStats;

    [Header("사망 시 페이드 아웃")]
    [SerializeField] private float fadeDelay = 0.5f;
    [SerializeField] private float fadeDuration = 1f;

    private CanvasGroup canvasGroup;
    private bool fadingOut;

    private void Awake()
    {
        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStats>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        if (playerStats == null) return;

        playerStats.OnHealthChanged += UpdateBar;
        UpdateBar(playerStats.CurrentHp, playerStats.MaxHp);
    }

    private void OnDestroy()
    {
        if (playerStats != null)
            playerStats.OnHealthChanged -= UpdateBar;
    }

    private void UpdateBar(float current, float max)
    {
        if (hpSlider != null)
            hpSlider.value = current / max;

        if (current <= 0f && !fadingOut)
        {
            fadingOut = true;
            StartCoroutine(FadeOut());
        }
    }

    private IEnumerator FadeOut()
    {
        yield return new WaitForSecondsRealtime(fadeDelay);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }
}
