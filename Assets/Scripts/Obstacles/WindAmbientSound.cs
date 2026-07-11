using UnityEngine;

/// <summary>
/// Attach this to ANY GameObject in the scene to play a looping wind/fan ambient sound.
/// The sound starts immediately when the scene loads.
///
/// SETUP:
///   1. Select the Fan or Wind zone GameObject in the Hierarchy.
///   2. Drag WindAmbientSound onto it.
///   3. Drag Wind.mp3 into the "Wind Clip" field in the Inspector.
///      (Or leave empty — it will try AudioManager.wind automatically)
/// </summary>
public class WindAmbientSound : MonoBehaviour
{
    [Header("Audio — drag Wind.mp3 here")]
    public AudioClip windClip;
    [Range(0f, 1f)] public float volume = 0.45f;

    private AudioSource src;

    private void Awake()
    {
        // Build the AudioSource immediately in Awake
        src = gameObject.AddComponent<AudioSource>();
        src.loop        = true;
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.volume      = volume;
    }

    private void Start()
    {
        // Try AudioManager if no clip was assigned in Inspector
        if (windClip == null && AudioManager.Instance != null)
            windClip = AudioManager.Instance.wind;

        if (windClip == null)
        {
            Debug.LogWarning("[WindAmbientSound] No wind clip found! " +
                "Drag Wind.mp3 into the 'Wind Clip' field on " + gameObject.name, this);
            return;
        }

        Debug.Log("[WindAmbientSound] Playing wind loop on: " + gameObject.name, this);
        src.clip = windClip;
        src.Play();
    }
}
