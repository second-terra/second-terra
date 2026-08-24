using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class CharacterSelectManager : MonoBehaviour
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
        if (mechIndex < 0 ||
        mechIndex >= selectedFrames.Length ||
        mechIndex >= mechNames.Length ||
        mechIndex >= mechDescriptions.Length)
        {
            Debug.LogWarning("잘못된 의체 인덱스입니다: " + mechIndex);
            return;
        }

        selectedMech = mechIndex;

        for (int i = 0; i < selectedFrames.Length; i++)
        {
            selectedFrames[i].SetActive(i == selectedMech);
        }

        mechName.text = mechNames[mechIndex];
        mechDescription.text = mechDescriptions[mechIndex];

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
