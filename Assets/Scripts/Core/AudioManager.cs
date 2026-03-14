using UnityEngine;

public class AudioManager : MonoBehaviour {
    public static AudioManager Instance { get; private set; }

    [Header("BGM")]
    [SerializeField] AudioClip bgmClip;
    [SerializeField] [Range(0f, 1f)] float bgmVolume = 0.5f;

    [Header("SFX")]
    [SerializeField] AudioClip snapSFX;
    [SerializeField] [Range(0f, 1f)] float sfxVolume = 1f;

    AudioSource bgmSource;
    AudioSource sfxSource;

    void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.volume = bgmVolume;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.volume = sfxVolume;

        PlayBGM();
    }

    void PlayBGM() {
        if (bgmClip == null) return;
        bgmSource.clip = bgmClip;
        bgmSource.Play();
    }

    // 播放吸附音效（或任意传入的音效片段）
    public void PlaySnap() => PlaySFX(snapSFX);

    public void PlaySFX(AudioClip clip) {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    public void SetBGMVolume(float volume) {
        bgmVolume = Mathf.Clamp01(volume);
        bgmSource.volume = bgmVolume;
    }

    public void SetSFXVolume(float volume) {
        sfxVolume = Mathf.Clamp01(volume);
        sfxSource.volume = sfxVolume;
    }
}
