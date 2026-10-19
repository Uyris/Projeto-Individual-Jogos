using UnityEngine;

/// <summary>Toca a música da cena e efeitos sonoros gerais (interface, alarmes, etc).</summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioClip music;
    [Range(0f, 1f)] public float musicVolume = 0.45f;

    [Header("Interface")]
    public AudioClip clickClip;
    public AudioClip selectClip;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        if (music == null) return;
        musicSource.clip = music;
        musicSource.loop = true;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public static void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (Instance != null && clip != null) Instance.sfxSource.PlayOneShot(clip, volume);
    }

    public static void PlayUi(bool click)
    {
        if (Instance != null) PlaySfx(click ? Instance.clickClip : Instance.selectClip, click ? 0.8f : 0.5f);
    }

    public static void StopMusic()
    {
        if (Instance != null) Instance.musicSource.Stop();
    }

    public static void DuckMusic(bool duck)
    {
        if (Instance != null) Instance.musicSource.volume = Instance.musicVolume * (duck ? 0.35f : 1f);
    }
}
