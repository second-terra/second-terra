using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// 옵션 패널. 메인메뉴와 인게임 일시정지 양쪽에서 같은 프리팹을 쓴다.
// 값 자체는 GameSettings 가 보관하므로 이 스크립트는 UI 와 설정을 잇는 역할만 한다.
//
// 인게임에서는 Time.timeScale 이 0 인 상태로 열리므로, 시간에 의존하는 연출은 넣지 않는다.
// (uGUI 입력은 timeScale 과 무관하게 동작한다)
public class OptionsPanel : MonoBehaviour
{
    [Header("볼륨 슬라이더 (Min 0 / Max 1)")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("볼륨 수치 표시 (선택 - 비워둬도 동작함)")]
    [SerializeField] private TMP_Text masterValueText;
    [SerializeField] private TMP_Text bgmValueText;
    [SerializeField] private TMP_Text sfxValueText;

    [Header("화면")]
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    [Header("닫기")]
    [SerializeField] private Button closeButton;

    [Tooltip("인게임에서는 PauseManager.CloseOptions() 를 연결해 일시정지 창으로 돌아가게 한다. " +
             "메인메뉴에서는 비워두면 패널만 닫힌다.")]
    [SerializeField] private UnityEvent onClosed;

    // 드롭다운 인덱스 → 실제 해상도
    private readonly List<Vector2Int> resolutions = new List<Vector2Int>();

    // 패널을 열면서 UI 에 현재값을 채워 넣을 때 onValueChanged 가 되도는 것을 막는다.
    private bool applyingValues;

    private void Awake()
    {
        BuildResolutionList();

        if (masterSlider != null) masterSlider.onValueChanged.AddListener(OnMasterChanged);
        if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(OnBgmChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        if (resolutionDropdown != null) resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    // 패널이 열릴 때마다 현재 설정으로 UI 를 다시 맞춘다.
    // (첫 열림에는 저장된 값을, 이후에는 다른 경로로 바뀌었을 수 있는 값을 반영)
    private void OnEnable()
    {
        applyingValues = true;

        if (masterSlider != null) masterSlider.value = GameSettings.MasterVolume;
        if (bgmSlider != null) bgmSlider.value = GameSettings.BgmVolume;
        if (sfxSlider != null) sfxSlider.value = GameSettings.SfxVolume;
        if (fullscreenToggle != null) fullscreenToggle.isOn = GameSettings.Fullscreen;
        if (resolutionDropdown != null) resolutionDropdown.value = CurrentResolutionIndex();

        applyingValues = false;

        RefreshVolumeTexts();
    }

    // 닫기 버튼, ESC(PauseManager), 씬 전환 등 어떤 경로로 닫히든 여기서 한 번 저장한다.
    // 슬라이더 드래그 중에는 저장하지 않으므로 디스크 쓰기는 이 시점에만 발생한다.
    private void OnDisable()
    {
        GameSettings.Save();
    }

    // 닫기 버튼과 외부(메인메뉴 등) 양쪽에서 부를 수 있게 public
    public void Close()
    {
        gameObject.SetActive(false);
        onClosed?.Invoke();
    }

    public void Open()
    {
        gameObject.SetActive(true);
    }

    private void OnMasterChanged(float value)
    {
        if (applyingValues) return;
        GameSettings.MasterVolume = value;
        RefreshVolumeTexts();
    }

    private void OnBgmChanged(float value)
    {
        if (applyingValues) return;
        GameSettings.BgmVolume = value;
        RefreshVolumeTexts();
    }

    private void OnSfxChanged(float value)
    {
        if (applyingValues) return;
        GameSettings.SfxVolume = value;
        RefreshVolumeTexts();
    }

    private void OnFullscreenChanged(bool isOn)
    {
        if (applyingValues) return;
        GameSettings.Fullscreen = isOn;
    }

    private void OnResolutionChanged(int index)
    {
        if (applyingValues) return;
        if (index < 0 || index >= resolutions.Count) return;

        GameSettings.SetResolution(resolutions[index].x, resolutions[index].y);
    }

    // Screen.resolutions 는 같은 해상도가 주사율별로 여러 번 들어오므로 가로x세로 기준으로 중복을 없앤다.
    private void BuildResolutionList()
    {
        resolutions.Clear();

        foreach (Resolution r in Screen.resolutions)
        {
            var size = new Vector2Int(r.width, r.height);
            if (!resolutions.Contains(size))
                resolutions.Add(size);
        }

        // 에디터에서는 목록이 비거나 현재 해상도가 빠져 있을 수 있어, 최소한 현재값은 고를 수 있게 넣어준다.
        var current = new Vector2Int(Screen.width, Screen.height);
        if (!resolutions.Contains(current))
            resolutions.Add(current);

        resolutions.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));

        if (resolutionDropdown == null) return;

        var labels = new List<string>(resolutions.Count);
        foreach (Vector2Int r in resolutions)
            labels.Add($"{r.x} x {r.y}");

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(labels);
    }

    private int CurrentResolutionIndex()
    {
        int index = resolutions.IndexOf(new Vector2Int(Screen.width, Screen.height));
        return index >= 0 ? index : 0;
    }

    private void RefreshVolumeTexts()
    {
        SetPercentText(masterValueText, GameSettings.MasterVolume);
        SetPercentText(bgmValueText, GameSettings.BgmVolume);
        SetPercentText(sfxValueText, GameSettings.SfxVolume);
    }

    private static void SetPercentText(TMP_Text text, float value01)
    {
        if (text != null)
            text.text = Mathf.RoundToInt(value01 * 100f) + "%";
    }
}
