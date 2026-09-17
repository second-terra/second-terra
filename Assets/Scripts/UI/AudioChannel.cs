using UnityEngine;

// AudioSource 하나를 BGM/효과음 채널 볼륨에 연동시킨다.
// 옵션에서 슬라이더를 움직이면 GameSettings 가 이벤트를 쏘고 여기서 즉시 반영한다.
//
// 마스터 볼륨은 AudioListener 로 전역 적용되지만 채널별 볼륨은 그런 수단이 없어서,
// 소리를 내는 AudioSource 마다 이 컴포넌트를 붙여주는 방식으로 처리한다.
// (지금 프로젝트에서 AudioSource 를 쓰는 건 RangedBeamEnemy 하나뿐이다)
[RequireComponent(typeof(AudioSource))]
public class AudioChannel : MonoBehaviour
{
    public enum Channel { Bgm, Sfx }

    [SerializeField] private Channel channel = Channel.Sfx;

    [Tooltip("이 소리 자체의 기본 크기. 채널 볼륨과 곱해진다. (0.5 면 항상 채널 볼륨의 절반)")]
    [Range(0f, 1f)]
    [SerializeField] private float baseVolume = 1f;

    private AudioSource source;

    // 컴포넌트를 처음 붙일 때 AudioSource 에 이미 맞춰둔 볼륨을 기본값으로 가져온다.
    private void Reset()
    {
        AudioSource existing = GetComponent<AudioSource>();
        if (existing != null)
            baseVolume = existing.volume;
    }

    private void Awake()
    {
        source = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        GameSettings.OnVolumeChanged += Apply;
        Apply();
    }

    private void OnDisable()
    {
        GameSettings.OnVolumeChanged -= Apply;
    }

    private void Apply()
    {
        if (source == null) return;

        float channelVolume = channel == Channel.Bgm ? GameSettings.BgmVolume : GameSettings.SfxVolume;
        source.volume = baseVolume * channelVolume;
    }
}
