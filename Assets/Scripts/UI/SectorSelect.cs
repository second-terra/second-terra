using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SectorSelect : MonoBehaviour
{
    public void SelectSector(int sectorNumber)
    {
        PlayerPrefs.SetInt("SelectedSector", sectorNumber);
        PlayerPrefs.Save();

        SceneManager.LoadScene("CharacterSelectScene");
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene("MainmenuScene");
    }
}