using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private float yOffset = 2.2f;

    private Camera mainCam;

    private void Awake()
    {
        mainCam = Camera.main;
        SetVisible(false);
    }

    private void LateUpdate()
    {
        if (worldCanvas == null || mainCam == null) return;

        Transform followTarget = transform.parent != null ? transform.parent : transform;
        worldCanvas.transform.position = followTarget.position + Vector3.up * yOffset;
        worldCanvas.transform.forward = mainCam.transform.forward;
    }

    public void UpdateBar(float current, float max)
    {
        if (hpSlider != null)
            hpSlider.value = current / max;

        SetVisible(true);
    }

    private void SetVisible(bool visible)
    {
        if (worldCanvas != null)
            worldCanvas.enabled = visible;
    }
}
