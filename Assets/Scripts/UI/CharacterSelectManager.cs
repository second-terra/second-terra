using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MechSelectManager : MonoBehaviour
{
    public GameObject[] selectedFrames;

    public TMP_Text mechName;
    public TMP_Text mechDescription;

    public string[] mechNames;
    [TextArea(3, 6)]
    public string[] mechDescriptions;

    private int selectedMech = -1;

    public void SelectMech(int mechIndex)
    {
        selectedMech = mechIndex;

        for (int i = 0; i < selectedFrames.Length; i++)
        {
            selectedFrames[i].SetActive(i == selectedMech);
        }

        mechName.text = mechNames[mechIndex];
        mechDescription.text = mechDescriptions[mechIndex];

        Debug.Log("선택한 의체: " + selectedMech);
    }

    public void StartGame()
    {
        if (selectedMech == -1)
        {
            Debug.Log("의체를 선택해주세요.");
            return;
        }

        PlayerPrefs.SetInt("SelectedMech", selectedMech);
        PlayerPrefs.Save();

        SceneManager.LoadScene("GameScene");
    }
}
