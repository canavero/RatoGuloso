using System.Collections.Generic;
using UnityEngine;

// Toca música e efeitos sonoros. É criado sozinho na primeira vez que é usado e
// sobrevive à troca de cenas (DontDestroyOnLoad). Os sons ficam em Assets/Resources/Audio
// e são chamados pelo nome do arquivo, ex.: AudioManager.PlaySfx("coletar").
public class AudioManager : MonoBehaviour
{
    const string MutedKey = "RatoGuloso_Muted";

    static AudioManager instance;

    AudioSource musicSource;
    AudioSource[] sfxSources;
    int nextSfx;
    readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

    public static bool Muted { get; private set; }

    static AudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("AudioManager");
                instance = go.AddComponent<AudioManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = 0.45f;

        // vários AudioSources para os efeitos poderem tocar ao mesmo tempo
        sfxSources = new AudioSource[6];
        for (int i = 0; i < sfxSources.Length; i++)
        {
            sfxSources[i] = gameObject.AddComponent<AudioSource>();
            sfxSources[i].playOnAwake = false;
        }

        foreach (AudioClip clip in Resources.LoadAll<AudioClip>("Audio"))
            clips[clip.name] = clip;

        Muted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
        AudioListener.volume = Muted ? 0f : 1f;
    }

    public static void PlaySfx(string name, float volume = 1f, float pitchVariation = 0.06f)
    {
        AudioManager am = Instance;
        if (!am.clips.TryGetValue(name, out AudioClip clip))
        {
            Debug.LogWarning("Som não encontrado em Resources/Audio: " + name);
            return;
        }
        AudioSource src = am.sfxSources[am.nextSfx];
        am.nextSfx = (am.nextSfx + 1) % am.sfxSources.Length;
        src.clip = clip;
        src.volume = volume;
        src.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        src.Play();
    }

    public static void PlayMusic(string name)
    {
        AudioManager am = Instance;
        if (!am.clips.TryGetValue(name, out AudioClip clip))
        {
            Debug.LogWarning("Música não encontrada em Resources/Audio: " + name);
            return;
        }
        if (am.musicSource.clip == clip && am.musicSource.isPlaying) return;
        am.musicSource.clip = clip;
        am.musicSource.Play();
    }

    public static void StopMusic()
    {
        if (instance != null) instance.musicSource.Stop();
    }

    public static void ToggleMute()
    {
        Instance.SetMuted(!Muted);
    }

    void SetMuted(bool muted)
    {
        Muted = muted;
        AudioListener.volume = muted ? 0f : 1f;
        PlayerPrefs.SetInt(MutedKey, muted ? 1 : 0);
        PlayerPrefs.Save();
    }
}
