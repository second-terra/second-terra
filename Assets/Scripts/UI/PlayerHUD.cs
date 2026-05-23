using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private Slider hpBar;
    [SerializeField] private PlayerStats playerStats;

    private void Start()
    {
        playerStats.onHpChanged.AddListener(UpdateHpBar);
        UpdateHpBar(playerStats.CurrentHp, playerStats.MaxHp);
    }

    private void UpdateHpBar(float current, float max)
    {
        hpBar.value = current / max;
    }
}
