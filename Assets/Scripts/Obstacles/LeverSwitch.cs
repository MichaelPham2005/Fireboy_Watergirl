using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class LeverSwitch : MonoBehaviour
{
    [Header("Gates")]
    [Tooltip("List of gates to open/close when this lever is toggled.")]
    public Gate[] connectedGates;

    [Header("Angle Settings")]
    [Tooltip("The exact Z rotation when leaning right (e.g., 0 or 45).")]
    public float rightAngle = 0f;
    
    [Tooltip("The exact Z rotation when leaning left (e.g., 90 or 135).")]
    public float leftAngle = 90f;

    [Tooltip("If true, pushing the lever left (to leftAngle) Opens the gates. If false, rightAngle Opens the gates.")]
    public bool leftIsON = true;

    [Header("Physics Settings")]
    [Tooltip("How fast the lever rotates when the player pushes it (must be higher than Snap Speed).")]
    public float pushSpeed = 250f;
    
    [Tooltip("How fast the lever falls back to the nearest side when released.")]
    public float snapSpeed = 100f;

    // t goes from 0 (rightAngle) to 1 (leftAngle)
    private float t = 0f; 
    private bool isLeftState; 
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic; // Must be Kinematic to block players!
        rb.useFullKinematicContacts = true; // Ensures it detects collisions with dynamic players

        // 1. Read initial angle and snap to nearest valid state
        float zRot = transform.localEulerAngles.z;
        zRot = (zRot % 360 + 360) % 360;

        float distToRight = Mathf.Abs(Mathf.DeltaAngle(zRot, rightAngle));
        float distToLeft = Mathf.Abs(Mathf.DeltaAngle(zRot, leftAngle));

        if (distToLeft < distToRight)
        {
            t = 1f;
            isLeftState = true;
        }
        else
        {
            t = 0f;
            isLeftState = false;
        }

        ApplyRotation();
        UpdateGates(instant: true);
    }

    private bool isBeingPushed = false;

    void OnCollisionStay2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Fireboy") || col.gameObject.CompareTag("Watergirl"))
        {
            float playerPosX = col.transform.position.x;
            float leverPosX = transform.position.x;

            // Get player's active input to ensure they are intentionally pushing,
            // not just standing on top or falling onto it.
            StandardPlayerMovement pMove = col.gameObject.GetComponent<StandardPlayerMovement>();
            float playerInput = pMove != null ? pMove.GetHorizontalInput() : 0f;
            
            float angularDistance = Mathf.Max(1f, Mathf.Abs(Mathf.DeltaAngle(rightAngle, leftAngle)));
            float step = (pushSpeed / angularDistance) * Time.fixedDeltaTime;

            // Player is on the right, actively pushing left
            if (playerPosX > leverPosX && playerInput < -0.1f)
            {
                t += step;
                isBeingPushed = true;
            }
            // Player is on the left, actively pushing right
            else if (playerPosX < leverPosX && playerInput > 0.1f)
            {
                t -= step;
                isBeingPushed = true;
            }

            t = Mathf.Clamp01(t);
        }
    }

    void FixedUpdate()
    {
        // Only apply snap gravity if no one is currently pushing it
        if (!isBeingPushed)
        {
            if (t > 0f && t < 1f)
            {
                float targetT = (t >= 0.5f) ? 1f : 0f;
                float angularDistance = Mathf.Max(1f, Mathf.Abs(Mathf.DeltaAngle(rightAngle, leftAngle)));
                float step = (snapSpeed / angularDistance) * Time.fixedDeltaTime;

                t = Mathf.MoveTowards(t, targetT, step);
            }
        }

        // Reset push flag for the next physics frame
        isBeingPushed = false;

        // Apply rotation exactly once per physics frame to avoid jitter
        ApplyRotation();

        // Check if we crossed the midpoint and need to commit to a new state
        bool newState = (t >= 0.5f);
        if (newState != isLeftState)
        {
            isLeftState = newState;
            UpdateGates(instant: false);
        }
    }

    private void ApplyRotation()
    {
        float currentAngle = Mathf.LerpAngle(rightAngle, leftAngle, t);
        
        if (rb != null)
        {
            // Use MoveRotation for smooth physics interaction with the player's dynamic Rigidbody
            rb.MoveRotation(currentAngle);
        }
        else
        {
            transform.localEulerAngles = new Vector3(0, 0, currentAngle);
        }
    }

    private void UpdateGates(bool instant)
    {
        bool open = leftIsON ? isLeftState : !isLeftState;
        
        foreach (var gate in connectedGates)
        {
            if (gate != null)
            {
                gate.SetState(open, instant);
            }
        }
    }
}
