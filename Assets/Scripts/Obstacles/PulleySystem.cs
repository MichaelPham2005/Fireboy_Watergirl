using UnityEngine;
using Fusion;
using Network;

/// <summary>
/// Pulley system using LineRenderers instead of a heavy object pool.
/// Now inherits from NetworkBehaviour to support online sync.
/// </summary>
public class PulleySystem : NetworkBehaviour
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
    public float linkSpacing = 0.64f;
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

    [Networked] public float NetworkOffset { get; set; }

    // Line Renderers
    private LineRenderer lineLeft;
    private LineRenderer lineRight;
    private LineRenderer lineTop;

    private void Start()
    {
        rbA = platformA.GetComponent<Rigidbody2D>();
        rbB = platformB.GetComponent<Rigidbody2D>();
        
        rbA.interpolation = RigidbodyInterpolation2D.Interpolate;
        rbB.interpolation = RigidbodyInterpolation2D.Interpolate;
        
        startYA = rbA.position.y;
        startYB = rbB.position.y;

        float maxDropA = GetFloorDistance(platformA);
        float maxDropB = GetFloorDistance(platformB);

        float maxRiseB = pulleyPointB.position.y - startYB - pulleyBuffer;
        maxOffsetPos = Mathf.Min(maxRiseB, maxDropA);

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
    }

    private float GetFloorDistance(PulleyPlatform platform)
    {
        Collider2D col = platform.GetComponent<Collider2D>();
        if (col == null) return 10f;
        
        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = false;
        RaycastHit2D[] results = new RaycastHit2D[10];
        
        int count = col.Cast(Vector2.down, filter, results, 100f);
        for (int i = 0; i < count; i++)
        {
            if (results[i].collider.gameObject != platform.gameObject && 
                !results[i].collider.CompareTag("Fireboy") && 
                !results[i].collider.CompareTag("Watergirl"))
            {
                return results[i].distance;
            }
        }
        return 10f; 
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
        lr.sortingOrder = -5;
        return lr;
    }

    private void FixedUpdate()
    {
        if (GameModeManager.CurrentMode == GameModeManager.GameMode.LocalCoop)
        {
            ProcessMovement(Time.fixedDeltaTime);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (GameModeManager.CurrentMode == GameModeManager.GameMode.OnlineMultiplayer)
        {
            if (HasStateAuthority)
            {
                ProcessMovement(Runner.DeltaTime);
                NetworkOffset = currentOffset;
            }
            else
            {
                // Client synchronizes to host offset
                currentOffset = Mathf.Lerp(currentOffset, NetworkOffset, Runner.DeltaTime * 15f);
                rbA.MovePosition(new Vector2(rbA.position.x, startYA - currentOffset));
                rbB.MovePosition(new Vector2(rbB.position.x, startYB + currentOffset));
            }
        }
    }

    private void ProcessMovement(float deltaTime)
    {
        float massDiff = platformA.totalMassOnPlatform - platformB.totalMassOnPlatform;
        
        // Giới hạn sự chênh lệch khối lượng ở mức thấp hơn (1.5) để ròng rọc không gia tốc quá lố
        float clampedMassDiff = Mathf.Clamp(massDiff, -1.5f, 1.5f);
        
        // Nhân thêm 0.5f vào moveSpeed để tổng tốc độ di chuyển chậm lại một nửa
        float targetVelocity = clampedMassDiff * (moveSpeed * 0.5f);
        
        currentVelocity = Mathf.Lerp(currentVelocity, targetVelocity, acceleration * deltaTime);

        float nextOffset = currentOffset + currentVelocity * deltaTime;
        
        // Clamp offset and halt velocity if we hit the min/max limits
        if (nextOffset >= maxOffsetPos)
        {
            nextOffset = maxOffsetPos;
            currentVelocity = 0f;
        }
        else if (nextOffset <= maxOffsetNeg)
        {
            nextOffset = maxOffsetNeg;
            currentVelocity = 0f;
        }

        currentOffset = nextOffset;

        // Chỉ dùng MovePosition để di chuyển Kinematic Body một cách mượt mà tuyệt đối.
        // Bỏ linearVelocity vì nó bị conflict với script di chuyển của nhân vật (nhân vật cũng đang set linearVelocity)
        rbA.MovePosition(new Vector2(rbA.position.x, startYA - currentOffset));
        rbB.MovePosition(new Vector2(rbB.position.x, startYB + currentOffset));
    }

    private void Update()
    {
        // Use currentOffset for visuals (which clients manually sync from NetworkOffset)
        Vector3 posA = platformA.transform.position;
        lineLeft.SetPosition(0, pulleyPointA.position);
        lineLeft.SetPosition(1, posA);
        float distLeft = Vector3.Distance(pulleyPointA.position, posA);
        float scaleLeft = distLeft / chainTextureWorldLength;
        lineLeft.material.mainTextureScale = new Vector2(scaleLeft, 1f);
        lineLeft.material.mainTextureOffset = new Vector2(-scaleLeft, 0f);

        Vector3 posB = platformB.transform.position;
        lineRight.SetPosition(0, pulleyPointB.position);
        lineRight.SetPosition(1, posB);
        float distRight = Vector3.Distance(pulleyPointB.position, posB);
        float scaleRight = distRight / chainTextureWorldLength;
        lineRight.material.mainTextureScale = new Vector2(scaleRight, 1f);
        lineRight.material.mainTextureOffset = new Vector2(-scaleRight, 0f);

        float offset = (currentOffset / chainTextureWorldLength) * textureScrollMultiplier;
        lineTop.material.mainTextureOffset = new Vector2(offset, 0);
    }
}
