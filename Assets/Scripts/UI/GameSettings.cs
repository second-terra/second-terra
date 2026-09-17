using System;
using UnityEngine;

// 게임 전역 설정 저장소. 옵션 패널이 값을 바꾸면 여기로 들어오고, PlayerPrefs 에 저장돼
// 다음 실행에도 유지된다. 메인메뉴와 인게임이 같은 값을 봐야 하고 씬을 넘나들어도
// 살아있어야 해서 MonoBehaviour 가 아닌 static 클래스로 둔다.
//
// 마스터 볼륨만 AudioListener 로 즉시 적용된다. BGM/효과음은 아직 사운드 시스템이 없어서
// 값만 보관하며, AudioSource 에 AudioChannel 을 붙이면 해당 채널 볼륨을 따라간다.
public static class GameSettings
{
    private const string MasterKey     = "Settings.MasterVolume";
    private const string BgmKey        = "Settings.BgmVolume";
    private const string SfxKey        = "Settings.SfxVolume";
    private const string FullscreenKey = "Settings.Fullscreen";
    private const string ResWidthKey   = "Settings.ResolutionWidth";
    private const string ResHeightKey  = "Settings.ResolutionHeight";

    // BGM/효과음 볼륨이 바뀔 때 발생. AudioChannel 이 구독해서 자기 AudioSource 에 반영한다.
    public static event Action OnVolumeChanged;

    private static float master = 1f;
    private static float bgm = 1f;
    private static float sfx = 1f;

    public static float MasterVolume
    {
        get => master;
        set
        {
            master = Mathf.Clamp01(value);
            AudioListener.volume = master;   // 전역이라 사운드 시스템 없이도 바로 먹는다
            PlayerPrefs.SetFloat(MasterKey, master);
            OnVolumeChanged?.Invoke();
        }
    }

    public static float BgmVolume
    {
        get => bgm;
        set
        {
            bgm = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(BgmKey, bgm);
            OnVolumeChanged?.Invoke();
        }
    }

    public static float SfxVolume
    {
        get => sfx;
        set
        {
            sfx = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxKey, sfx);
            OnVolumeChanged?.Invoke();
        }
    }

    public static bool Fullscreen
    {
        get => Screen.fullScreen;
        set
        {
            Screen.fullScreen = value;
            PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
        }
    }

    public static void SetResolution(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            Debug.LogWarning($"[GameSettings] 잘못된 해상도입니다: {width}x{height}");
            return;
        }

        Screen.SetResolution(width, height, Screen.fullScreen);
        PlayerPrefs.SetInt(ResWidthKey, width);
        PlayerPrefs.SetInt(ResHeightKey, height);
    }

    // 슬라이더를 드래그하는 동안 매 프레임 디스크에 쓰지 않도록, 값 변경은 메모리에만 하고
    // 실제 저장은 옵션 패널이 닫힐 때 한 번만 부른다.
    public static void Save() => PlayerPrefs.Save();

    // 어느 씬에서 플레이를 시작하든(에디터에서 GameScene 을 바로 여는 경우 포함)
    // 저장된 설정이 적용된 상태로 출발하도록 씬 로드 전에 한 번 불린다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Load()
    {
        master = PlayerPrefs.GetFloat(MasterKey, 1f);
        bgm    = PlayerPrefs.GetFloat(BgmKey, 1f);
        sfx    = PlayerPrefs.GetFloat(SfxKey, 1f);

        AudioListener.volume = master;

        // Screen.fullScreen 은 대입해도 현재 프레임이 끝날 때 반영되므로, 곧바로 다시 읽으면
        // 아직 이전 모드가 나온다. 그래서 SetResolution 에 Screen.fullScreen 을 넘기면
        // 방금 요청한 창 모드가 이전 모드로 덮어써진다. 저장값을 변수에 담아 직접 넘긴다.
        bool fullscreen = PlayerPrefs.HasKey(FullscreenKey)
            ? PlayerPrefs.GetInt(FullscreenKey) == 1
            : Screen.fullScreen;

        int width  = PlayerPrefs.GetInt(ResWidthKey, 0);
        int height = PlayerPrefs.GetInt(ResHeightKey, 0);

        // 해상도 저장값이 있으면 창 모드까지 한 번에 적용하고, 없으면 창 모드만 적용한다.
        if (width > 0 && height > 0)
            Screen.SetResolution(width, height, fullscreen);
        else
            Screen.fullScreen = fullscreen;
    }
}
