using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MissionUI : MonoBehaviour
{
    public TMP_Text missionText;
    LayoutElement layout;
    RectTransform rect;

    CanvasGroup canvasGroup;

    public void SetText(string text)
    {
        missionText.text = text;
    }

    public void SetColor(Color color)
    {
        missionText.color = color;
    }

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        layout = GetComponent<LayoutElement>();
        rect = GetComponent<RectTransform>();
    }

    public void Show()
    {
        gameObject.SetActive(true);

        StopAllCoroutines();
        StartCoroutine(Blink());
    }

    IEnumerator Blink()
    {
        for (int i = 0; i < 3; i++)
        {
            // 천천히 어두워짐
            while (canvasGroup.alpha > 0.5f)
            {
                canvasGroup.alpha -= Time.deltaTime * 3f;
                yield return null;
            }

            // 천천히 밝아짐
            while (canvasGroup.alpha < 1f)
            {
                canvasGroup.alpha += Time.deltaTime * 3f;
                yield return null;
            }
        }

        canvasGroup.alpha = 1f;
    }

    IEnumerator CompleteAnimation()
    {
        missionText.color = Color.green;

        Vector2 start = rect.anchoredPosition;

        Vector2 end = start + new Vector2(180, 0);

        float startHeight = layout.preferredHeight;

        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * 3;

            rect.anchoredPosition =
                Vector2.Lerp(start, end, t);

            canvasGroup.alpha =
                Mathf.Lerp(1, 0, t);

            layout.preferredHeight =
                Mathf.Lerp(startHeight, 0, t);

            yield return null;
        }

        gameObject.SetActive(false);
    }

    public void Complete()
    {
        StartCoroutine(CompleteAnimation());
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
