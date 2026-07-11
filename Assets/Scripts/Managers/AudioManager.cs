using UnityEngine;

/// <summary>
/// Central singleton audio manager for Fireboy & Watergirl.
/// Persists across all scenes. Manages background music (BGM) and sound effects (SFX).
/// 
/// SETUP:
///   1. Create an empty GameObject named "AudioManager" in your first scene.
///   2. Attach this script to it.
///   3. Drag all audio files from Assets/Audio into the matching fields in the Inspector.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Volume")]
    [Range(0f, 1f)] public float bgmVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    // --- MUSIC ---
    [Header("Music")]
    public AudioClip menuMusic;
    public AudioClip levelMusic;
    public AudioClip levelMusicDark;
    public AudioClip levelMusicSpeed;
    public AudioClip levelMusicFinish;
    public AudioClip levelMusicFinishSpeed;
    public AudioClip levelMusicOver;

    // --- SFX: PLAYERS ---
    [Header("SFX - Player")]
    public AudioClip jumpFireboy;
    public AudioClip jumpWatergirl;
    public AudioClip steps;
    public AudioClip iceStepsFireboy;
    public AudioClip iceStepsWatergirl;
    public AudioClip waterSteps;
    public AudioClip death;

    // --- SFX: COLLECTIBLES ---
    [Header("SFX - Collectibles")]
    public AudioClip diamond;
    public AudioClip endDiamond;

    // --- SFX: LEVEL EVENTS ---
    [Header("SFX - Level Events")]
    public AudioClip door;
    public AudioClip endPass;
    public AudioClip endFail;
    public AudioClip clock;

    // --- SFX: OBSTACLES & INTERACTABLES ---
    [Header("SFX - Obstacles")]
    public AudioClip lever;
    public AudioClip platform;
    public AudioClip platformCorrupt;
    public AudioClip pusher;
    public AudioClip lightPusher;
    public AudioClip freeze;
    public AudioClip melt;
    public AudioClip portalOpen;
    public AudioClip portalClose;
    public AudioClip portalLoop;
    public AudioClip portalTransport;
    public AudioClip slider;
    public AudioClip wind;

    // --- SFX: UI ---
    [Header("SFX - UI")]
    public AudioClip charToggle1;
    public AudioClip charToggle2;

    // Internal audio sources
    private AudioSource bgmSource;
    private AudioSource sfxSource;

    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateAudioSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void CreateAudioSources()
    {
        // BGM: looping background music
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.volume = bgmVolume;
        bgmSource.playOnAwake = false;

        // SFX: short one-shot effects
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.volume = sfxVolume;
        sfxSource.playOnAwake = false;
    }

    // =========================================================================
    //  MUSIC CONTROL
    // =========================================================================

    /// <summary>Plays a BGM clip. Does nothing if the same clip is already playing.</summary>
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        bgmSource.volume = bgmVolume;
    }

    // =========================================================================
    //  SFX CONTROL
    // =========================================================================

    /// <summary>Plays a one-shot SFX clip at the given volume multiplier.</summary>
    public void PlaySFX(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume * volumeMultiplier);
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }

    // =========================================================================
    //  CONVENIENCE METHODS — call these from other scripts
    // =========================================================================

    // -- Player --
    public void PlayJump(bool isFireboy)
    {
        PlaySFX(isFireboy ? jumpFireboy : jumpWatergirl);
    }

    public void PlayDeath()         => PlaySFX(death);
    public void PlaySteps()         => PlaySFX(steps);
    public void PlayIceSteps(bool isFireboy) => PlaySFX(isFireboy ? iceStepsFireboy : iceStepsWatergirl);
    public void PlayWaterSteps()    => PlaySFX(waterSteps);

    // -- Collectibles --
    public void PlayDiamond()       => PlaySFX(diamond);
    public void PlayEndDiamond()    => PlaySFX(endDiamond);

    // -- Obstacles --
    public void PlayDoor()          => PlaySFX(door);
    public void PlayLever()         => PlaySFX(lever);
    public void PlayPlatform()      => PlaySFX(platform);
    public void PlayPusher()        => PlaySFX(pusher);
    public void PlayFreeze()        => PlaySFX(freeze);
    public void PlayMelt()          => PlaySFX(melt);
    public void PlayPortalOpen()    => PlaySFX(portalOpen);
    public void PlayPortalClose()   => PlaySFX(portalClose);
    public void PlayPortalTransport() => PlaySFX(portalTransport);

    // -- Level Events --
    public void PlayWin()
    {
        StopBGM();
        PlaySFX(endPass);
        // Play finish music after a short delay so SFX isn't cut off
        Invoke(nameof(PlayFinishMusic), 0.5f);
    }

    public void PlayLose()
    {
        StopBGM();
        PlaySFX(endFail);
        Invoke(nameof(PlayOverMusic), 0.5f);
    }

    private void PlayFinishMusic() => PlayBGM(levelMusicFinish);
    private void PlayOverMusic()   => PlayBGM(levelMusicOver);

    public void PlayLevelMusic()   => PlayBGM(levelMusic);
    public void PlayMenuMusic()    => PlayBGM(menuMusic);

    // -- UI --
    public void PlayCharToggle(bool isFirstToggle)
    {
        PlaySFX(isFirstToggle ? charToggle1 : charToggle2);
    }
}
