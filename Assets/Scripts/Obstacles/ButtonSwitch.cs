using UnityEngine;

public class ButtonSwitch : MonoBehaviour
{
    [Header("Gates")]
    [Tooltip("List of gates to open when this button is pressed.")]
    public Gate[] connectedGates;

    [Header("Detection Settings")]
    [Tooltip("The center of the detection box relative to the button.")]
    public Vector2 boxOffset = new Vector2(0f, 0.3f);
    
    [Tooltip("The size of the detection box. Should be slightly smaller than the button's width.")]
    public Vector2 boxSize = new Vector2(0.8f, 0.4f);
    
    [Tooltip("Layers that can press this button (e.g., Default, Player).")]
    public LayerMask detectableLayers = ~0; // Default to Everything

    [Header("Visual Settings")]
    [Tooltip("How far down the button moves when pressed.")]
    public float pressedYOffset = -0.15f;
    
    [Tooltip("How fast the button moves down and up.")]
    public float moveSpeed = 10f;

    private Vector3 initialLocalPos;
    private bool isPressed = false;

    void Start()
    {
        initialLocalPos = transform.localPosition;
        
        // Initialize gates to closed
        UpdateGates(false, true);
    }

    void FixedUpdate()
    {
        // 1. Detect objects using a virtual hitbox above the button
        Vector2 checkPos = (Vector2)transform.position + boxOffset;
        Collider2D[] cols = Physics2D.OverlapBoxAll(checkPos, boxSize, 0f, detectableLayers);

        bool foundValidObject = false;
        foreach (var col in cols)
        {
            // Ignore ourselves and ignore triggers
            if (col.gameObject != this.gameObject && !col.isTrigger)
            {
                // Must be a player or a rock (if you have tags for rocks)
                if (col.CompareTag("Fireboy") || col.CompareTag("Watergirl")) // add rock tag here
                {
                    foundValidObject = true;
                    break;
                }
            }
        }

        // 2. Handle State Change
        if (foundValidObject != isPressed)
        {
            isPressed = foundValidObject;
            UpdateGates(isPressed, false);
        }

        // 3. Visual Sinking (Lerp localPosition so it smoothly goes down)
        Vector3 targetPos = initialLocalPos;
        if (isPressed)
        {
            targetPos.y += pressedYOffset;
        }

        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, moveSpeed * Time.fixedDeltaTime);
    }

    private void UpdateGates(bool open, bool instant)
    {
        foreach (var gate in connectedGates)
        {
            if (gate != null)
            {
                gate.SetState(open, instant);
            }
        }
    }

    // Draw the detection box in the editor so it's easy to visualize and adjust
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector2 center = (Vector2)transform.position + boxOffset;
        Gizmos.DrawWireCube(center, boxSize);
    }
}
