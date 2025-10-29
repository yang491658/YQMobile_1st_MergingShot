using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum SoundType { BGM, SFX }

[System.Serializable]
public struct SoundClip
{
    public AudioClip[] bgmClips;
    public AudioClip[] sfxClips;
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { private set; get; }

    [Header("Source")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Volume")]
    [SerializeField][Range(0f, 1f)] private float bgmVolume = 1f;
    [SerializeField][Range(0f, 1f)] private float sfxVolume = 1f;
    private float prevBgmVolume;
    private float prevSfxVolume;
    public event System.Action<SoundType, float> OnChangeVolume;

    [Header("Clip")]
    [SerializeField] private SoundClip soundClips;
    private readonly Dictionary<string, AudioClip> bgmDict = new();
    private readonly Dictionary<string, AudioClip> sfxDict = new();

#if UNITY_EDITOR
    private void OnValidate()
    {
        var bgmList = new List<AudioClip>();
        LoadSound(bgmList, SoundType.BGM);
        soundClips.bgmClips = bgmList.ToArray();

        var sfxList = new List<AudioClip>();
        LoadSound(sfxList, SoundType.SFX);
        soundClips.sfxClips = sfxList.ToArray();
    }

    private static void LoadSound(List<AudioClip> _list, SoundType _type)
    {
        _list.Clear();
        string path = "Sounds/" + (_type == SoundType.BGM ? "BGMs" : "SFXs");
        var clips = Resources.LoadAll<AudioClip>(path);
        if (clips != null && clips.Length > 0)
            _list.AddRange(clips);
    }
#endif

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetAudio();

        foreach (var btn in FindObjectsByType<Button>(FindObjectsSortMode.None))
            btn.onClick.AddListener(Button);
    }

    public void Pause(bool _on) => AudioListener.pause = _on;
    public void Pause(string _on) => AudioListener.pause = bool.TryParse(_on, out var v) && v;

    #region 배경음
    public void PlayBGM(AudioClip _clip)
    {
        if (bgmSource == null || IsBGMMuted()) return;

        bgmSource.clip = _clip;
        bgmSource.Play();
    }

    public void PlayBGM(string _name)
    {
        if (bgmDict.TryGetValue(_name, out var clip))
            PlayBGM(clip);
    }

    public void StopBGM() => bgmSource.Stop();

    public void ToggleBGM()
    {
        if (!IsBGMMuted() && bgmVolume > 0f)
        {
            prevBgmVolume = bgmVolume;
            SetBGMVolume(0f);
        }
        else
        {
            SetBGMVolume(prevBgmVolume);
        }
    }
    #endregion

    #region 효과음
    public void PlaySFX(AudioClip _clip)
    {
        if (sfxSource == null || IsSFXMuted()) return;

        sfxSource.PlayOneShot(_clip);
    }

    public void PlaySFX(string _name)
    {
        if (sfxDict.TryGetValue(_name, out var clip))
            PlaySFX(clip);
    }

    public void ToggleSFX()
    {
        if (!IsSFXMuted() && sfxVolume > 0f)
        {
            prevSfxVolume = sfxVolume;
            SetSFXVolume(0f);
        }
        else
        {
            SetSFXVolume(prevSfxVolume);
        }
    }

    public void Button() => PlaySFX("Button");

    public void Shoot() => PlaySFX("Shoot");
    public void Merge(int _id = 0) => PlaySFX(_id != EntityManager.Instance.GetFinal() ? "Merge" : "Flame");
    public void Count() => PlaySFX("Count");

    public void GameOver()
    {
        PlaySFX("GameOver");
        PlayBGM("GameOver");
    }
    #endregion

    #region SET
    private void SetAudio()
    {
        if (bgmSource == null) bgmSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();

        bgmSource.playOnAwake = false;
        sfxSource.playOnAwake = false;
        bgmSource.loop = true;

        SetBGMVolume(bgmVolume);
        SetSFXVolume(sfxVolume);
        SetDictionaries();
    }

    public void SetBGMVolume(float _volume = 1f)
    {
        bgmVolume = Mathf.Clamp01(_volume);
        bgmSource.volume = bgmVolume;
        bgmSource.mute = (bgmVolume <= 0f);

        if (bgmVolume > 0f) prevBgmVolume = bgmVolume;

        OnChangeVolume?.Invoke(SoundType.BGM, bgmVolume);
    }

    public void SetSFXVolume(float _volume = 1f)
    {
        sfxVolume = Mathf.Clamp01(_volume);
        sfxSource.volume = sfxVolume;
        sfxSource.mute = (sfxVolume <= 0f);

        if (sfxVolume > 0f) prevSfxVolume = sfxVolume;

        OnChangeVolume?.Invoke(SoundType.SFX, sfxVolume);
    }

    private void SetDictionaries()
    {
        bgmDict.Clear();
        if (soundClips.bgmClips != null)
            foreach (var c in soundClips.bgmClips)
                if (c != null) bgmDict.TryAdd(c.name, c);

        sfxDict.Clear();
        if (soundClips.sfxClips != null)
            foreach (var c in soundClips.sfxClips)
                if (c != null) sfxDict.TryAdd(c.name, c);
    }
    #endregion

    #region GET
    public float GetBGMVolume() => bgmVolume;
    public float GetSFXVolume() => sfxVolume;
    public float GetSFXLength(string _name) =>
        sfxDict.TryGetValue(_name, out var _clip) && _clip != null ? _clip.length : 0f;

    public bool IsBGMMuted() => bgmSource != null && bgmSource.mute;
    public bool IsSFXMuted() => sfxSource != null && sfxSource.mute;
    #endregion
}
