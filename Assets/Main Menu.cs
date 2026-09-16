using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Tooltip("메인메뉴 캔버스 안의 옵션 패널. 시작할 때 자동으로 닫아둔다.")]
    [SerializeField] private GameObject optionsPanel;

    private void Start()
    {
        if (optionsPanel != null)
            optionsPanel.SetActive(false);
    }

    public void OnClickStartGame()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void OnClickOptions()
    {
        if (optionsPanel == null)
        {
            Debug.LogWarning("[MainMenu] OptionsPanel이 연결되지 않았습니다. 인스펙터에서 연결해주세요.");
            return;
        }

        optionsPanel.SetActive(true);
    }

    public void OnClickExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
