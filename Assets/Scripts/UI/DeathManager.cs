using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathManager : MonoBehaviour
{
    public GameObject deathPanel;

    public void ShowDeathPanel()
    {
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
