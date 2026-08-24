using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathManager : MonoBehaviour
{
    public GameObject deathPanel;

    public void ShowDeathPanel()
    {
        if (deathPanel == null)
        {
            Debug.LogWarning("DeathPanel이 연결되지 않았습니다.");
            return;
        }

        deathPanel.SetActive(true);
    }

    public void RestartSameMech()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void ChangeMech()
    {
        SceneManager.LoadScene("CharacterSelectScene");
    }
}
