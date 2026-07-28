using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MissionUI : MonoBehaviour
{
    public TMP_Text missionText;

    public Image icon;

    CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void Show()
    {
        gameObject.SetActive(true);

        StartCoroutine(Blink());
    }

    IEnumerator Blink()
    {
        for (int i = 0; i < 2; i++)
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

    public void Complete()
    {
        StartCoroutine(HideMission());
    }

    IEnumerator HideMission()
    {
        yield return new WaitForSeconds(0.5f);

        gameObject.SetActive(false);
    }
}
