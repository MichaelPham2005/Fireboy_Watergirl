using UnityEngine;

/// <summary>
/// Pulley system using LineRenderers instead of a heavy object pool.
/// </summary>
public class PulleySystem : MonoBehaviour
{
    [Header("Platforms")]
    public PulleyPlatform platformA;
    public PulleyPlatform platformB;

    [Header("Pulley Anchor Points")]
    public Transform pulleyPointA;
    public Transform pulleyPointB;

    [Header("Visual Settings")]
    [Tooltip("Material used for the left and right vertical chains")]
    public Material verticalChainMaterial;
    [Tooltip("Material used for the top horizontal chain")]
    public Material horizontalChainMaterial;
    
    public float chainTextureWorldLength = 2.76f;
    
    public float chainWidth = 0.5f;
    public float linkSpacing = 0.64f; // Keep for offset math if needed
    public float textureScrollMultiplier = 1f;

    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    [Tooltip("How fast the platform reaches max speed (higher = faster acceleration)")]
    public float acceleration = 10f;
    public float pulleyBuffer = 0.5f;

    // Internal state
    private float startYA, startYB;
    private Rigidbody2D rbA, rbB;
    private float currentOffset = 0f;
    private float currentVelocity = 0f;
    private float maxOffsetPos, maxOffsetNeg;

    // Line Renderers
    private LineRenderer lineLeft;
    private LineRenderer lineRight;
    private LineRenderer lineTop;

    // Audio
    [Header("Audio")]
    [Tooltip("Sound clip for when the pulley is moving. Leave empty to use AudioManager's default.")]
    public AudioClip pulleyMovingClip;
    [Range(0f, 1f)] public float pulleyVolume = 0.6f;
    private AudioSource pulleyAudioSource;
    private bool wasMoving = false;

    private void Start()
    {
        rbA = platformA.GetComponent<Rigidbody2D>();
        rbB = platformB.GetComponent<Rigidbody2D>();
        
        // Turn on Interpolation so the movement renders perfectly smooth at any framerate
        rbA.interpolation = RigidbodyInterpolation2D.Interpolate;
        rbB.interpolation = RigidbodyInterpolation2D.Interpolate;
        
        startYA = rbA.position.y;
        startYB = rbB.position.y;

        // Calculate how far down each platform can go before hitting the floor
        float maxDropA = GetFloorDistance(platformA);
        float maxDropB = GetFloorDistance(platformB);

        // Max positive offset (A goes down, B goes up)
        float maxRiseB = pulleyPointB.position.y - startYB - pulleyBuffer;
        maxOffsetPos = Mathf.Min(maxRiseB, maxDropA);

        // Max negative offset (A goes up, B goes down)
        float maxRiseA = pulleyPointA.position.y - startYA - pulleyBuffer;
        maxOffsetNeg = -Mathf.Min(maxRiseA, maxDropB);

        lineLeft = CreateLineRenderer("Line_Left", verticalChainMaterial);
        lineRight = CreateLineRenderer("Line_Right", verticalChainMaterial);
        lineTop = CreateLineRenderer("Line_Top", horizontalChainMaterial);
        
        float horizY = Mathf.Max(pulleyPointA.position.y, pulleyPointB.position.y);
        lineTop.SetPosition(0, new Vector3(pulleyPointA.position.x, horizY, 0));
        lineTop.SetPosition(1, new Vector3(pulleyPointB.position.x, horizY, 0));
        
        float distTop = Vector3.Distance(lineTop.GetPosition(0), lineTop.GetPosition(1));
        lineTop.material.mainTextureScale = new Vector2(distTop / chainTextureWorldLength, 1f);

        // Setup looping audio source for pulley movement
        pulleyAudioSource = gameObject.AddComponent<AudioSource>();
        pulleyAudioSource.loop = true;
        pulleyAudioSource.playOnAwake = false;
        pulleyAudioSource.spatialBlend = 0f;
        pulleyAudioSource.volume = 0f;

        // Use clip from Inspector if provided, otherwise fall back to AudioManager
        if (pulleyMovingClip == null && AudioManager.Instance != null)
            pulleyMovingClip = AudioManager.Instance.slider; // Slider is the closest SFX for a chain/pulley
        
        if (pulleyMovingClip != null)
            pulleyAudioSource.clip = pulleyMovingClip;
    }

    private float GetFloorDistance(PulleyPlatform platform)
    {
        Collider2D col = platform.GetComponent<Collider2D>();
        if (col == null) return 10f;
        
        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = false; // Ignore triggers
        RaycastHit2D[] results = new RaycastHit2D[10];
        
        // Cast the platform's collider straight down
        int count = col.Cast(Vector2.down, filter, results, 100f);
        for (int i = 0; i < count; i++)
        {
            // Ignore ourselves and players
            if (results[i].collider.gameObject != platform.gameObject && 
                !results[i].collider.CompareTag("Fireboy") && 
                !results[i].collider.CompareTag("Watergirl"))
            {
                // Return the distance to the floor
                return results[i].distance;
            }
        }
        return 10f; // Default if nothing is below
    }

    private LineRenderer CreateLineRenderer(string name, Material mat)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        if (mat != null) lr.material = mat;
        
        lr.startColor = Color.white;
        lr.endColor = Color.white;
        
        lr.widthMultiplier = chainWidth;
        lr.positionCount = 2;
        lr.textureMode = LineTextureMode.Stretch;
        lr.sortingOrder = -5; // Put chains behind platforms
        return lr;
    }

    private void FixedUpdate()
    {
        int weightDiff = platformA.playersOnPlatform - platformB.playersOnPlatform;
        float targetVelocity = weightDiff * moveSpeed;
        
        // Smoothly accelerate/decelerate towards the target velocity
        currentVelocity = Mathf.Lerp(currentVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);

        if (Mathf.Abs(currentVelocity) > 0.001f)
        {
            currentOffset += currentVelocity * Time.fixedDeltaTime;
            currentOffset = Mathf.Clamp(currentOffset, maxOffsetNeg, maxOffsetPos);
        }

        rbA.MovePosition(new Vector2(rbA.position.x, startYA - currentOffset));
        rbB.MovePosition(new Vector2(rbB.position.x, startYB + currentOffset));

        // Drive pulley audio
        bool isMoving = Mathf.Abs(currentVelocity) > 0.1f;
        if (pulleyAudioSource != null && pulleyAudioSource.clip != null)
        {
            if (isMoving && !wasMoving)
            {
                pulleyAudioSource.volume = pulleyVolume;
                pulleyAudioSource.Play();
            }
            else if (!isMoving && wasMoving)
            {
                // Fade out gracefully over 0.3s using LeanTween-free approach
                pulleyAudioSource.volume = 0f;
                pulleyAudioSource.Stop();
            }
        }
        wasMoving = isMoving;
    }

    private void Update()
    {
        // Update vertical lines
        Vector3 posA = platformA.transform.position;
        lineLeft.SetPosition(0, pulleyPointA.position);
        lineLeft.SetPosition(1, posA);
        float distLeft = Vector3.Distance(pulleyPointA.position, posA);
        float scaleLeft = distLeft / chainTextureWorldLength;
        lineLeft.material.mainTextureScale = new Vector2(scaleLeft, 1f);
        // Offset by -scale to anchor the texture at the bottom (platform) instead of the top (pulley)
        lineLeft.material.mainTextureOffset = new Vector2(-scaleLeft, 0f);

        Vector3 posB = platformB.transform.position;
        lineRight.SetPosition(0, pulleyPointB.position);
        lineRight.SetPosition(1, posB);
        float distRight = Vector3.Distance(pulleyPointB.position, posB);
        float scaleRight = distRight / chainTextureWorldLength;
        lineRight.material.mainTextureScale = new Vector2(scaleRight, 1f);
        lineRight.material.mainTextureOffset = new Vector2(-scaleRight, 0f);

        // Update horizontal line scroll offset
        // We use textureScrollMultiplier to control how fast it scrolls.
        float offset = (currentOffset / chainTextureWorldLength) * textureScrollMultiplier;
        lineTop.material.mainTextureOffset = new Vector2(offset, 0);
    }
}
