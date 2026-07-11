using UnityEngine;

/// <summary>
/// Attach this script to the seesaw BOARD (the rotating part with Rigidbody2D).
/// Plays a thump/creak sound whenever the seesaw rotates significantly.
///
/// SETUP:
///   1. In Hierarchy, expand Seesaw_System → find the child with Rigidbody2D
///      (usually the plank/board, NOT the base).
///   2. Drag SeesawSound onto that child GameObject.
///   3. Drag Platform.mp3 into the "Seesaw Clip" field in the Inspector.
///      (Or leave empty — it will try to auto-use AudioManager.platform)
/// </summary>
public class SeesawSound : MonoBehaviour
{
    [Header("Audio — drag Platform.mp3 here")]
    public AudioClip seesawClip;
    [Range(0f, 1f)] public float volume = 0.75f;

    [Header("Trigger Settings")]
    [Tooltip("Angular speed (deg/sec) above which the sound plays.")]
    public float angularSpeedThreshold = 8f;
    [Tooltip("Minimum seconds between two sounds.")]
    public float cooldown = 0.35f;

    private AudioSource src;
    private float lastSoundTime = -999f;

    private void Awake()
    {
        // Add a dedicated AudioSource on this object
        src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop       = false;
        src.spatialBlend = 0f;
        src.volume     = volume;
    }

    private void Start()
    {
        // If no clip dragged in, fall back to AudioManager
        if (seesawClip == null && AudioManager.Instance != null)
            seesawClip = AudioManager.Instance.platform;

        if (seesawClip == null)
            Debug.LogWarning("[SeesawSound] No clip assigned and AudioManager.platform is null!", this);
        else
            Debug.Log("[SeesawSound] Ready on: " + gameObject.name + " clip=" + seesawClip.name, this);
    }

    private void Update()
    {
        // Get the angular velocity of this object's Rigidbody2D
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null) return;

        float angularSpeed = Mathf.Abs(rb.angularVelocity);

        if (angularSpeed > angularSpeedThreshold && Time.time - lastSoundTime > cooldown)
        {
            src.clip = seesawClip;
            src.volume = volume;
            src.Play();
            lastSoundTime = Time.time;
        }
    }
}
