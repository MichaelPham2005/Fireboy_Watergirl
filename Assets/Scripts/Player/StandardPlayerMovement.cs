using UnityEngine;

public class StandardPlayerMovement : MonoBehaviour
{
    public enum PlayerType { Fireboy, Watergirl }

    [Header("Player Settings")]
    public PlayerType playerType = PlayerType.Fireboy;

    [Header("Movement Settings")]
    public float moveSpeed = 7f;
    public float jumpForce = 16f;

    [Header("Components")]
    private Rigidbody2D rb;
    private Collider2D col;
    public Animator bodyAnimator;
    public Animator headAnimator;

    [Header("Ground Detection")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.25f;
    public LayerMask groundLayer;
    private bool isGrounded;

    // Internal: track the surface normal of what we're standing on
    private Vector2 groundNormal = Vector2.up;
    private ContactPoint2D[] contacts = new ContactPoint2D[8];

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        rb.gravityScale = 3f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Fallback: if no ground layer assigned in Inspector, use Default
        if (((int)groundLayer) == 0)
            groundLayer = LayerMask.GetMask("Default");

        // Create zero-friction physics material at runtime
        // This removes the dependency on external .physicsMaterial2D asset files
        PhysicsMaterial2D noFriction = new PhysicsMaterial2D("Player_NoFriction")
        {
            friction = 0f,
            bounciness = 0f
        };
        if (col != null)
            col.sharedMaterial = noFriction;
    }

    void Update()
    {
        UpdateGroundState();

        float moveInput = GetHorizontalInput();
        bool jumpPressed = GetJumpInput();

        ApplyMovement(moveInput);

        // Animation
        if (bodyAnimator != null)
        {
            bodyAnimator.SetFloat("Speed", Mathf.Abs(moveInput));
            bodyAnimator.SetFloat("yVelocity", rb.linearVelocity.y);
            bodyAnimator.SetBool("IsGrounded", isGrounded);
        }
        
        if (headAnimator != null)
        {
            headAnimator.SetFloat("Speed", Mathf.Abs(moveInput));
            headAnimator.SetFloat("yVelocity", rb.linearVelocity.y);
            headAnimator.SetBool("IsGrounded", isGrounded);

            // Smoothly tilt the head based on vertical velocity when in the air
            float targetZRotation = 0f;
            
            // ONLY tilt if we have horizontal speed (i.e. moving jump). 
            // If jumping straight up (Speed < 0.1), keep the head perfectly stable (0 rotation).
            if (!isGrounded && Mathf.Abs(rb.linearVelocity.x) > 0.1f)
            {
                // Multiply y velocity to get an angle, clamp it so it doesn't rotate too far
                targetZRotation = Mathf.Clamp(rb.linearVelocity.y * 2.5f, -35f, 35f);
            }
            
            // Lerp the rotation for a smooth "curved" motion
            Quaternion targetRot = Quaternion.Euler(0, 0, targetZRotation);
            headAnimator.transform.localRotation = Quaternion.Lerp(
                headAnimator.transform.localRotation, 
                targetRot, 
                Time.deltaTime * 12f
            );

            // --- DEBUG: Log head animator state every 0.5 second ---
            if (Time.frameCount % 30 == 0)
            {
                var stateInfo = headAnimator.GetCurrentAnimatorStateInfo(0);
                var headSR = headAnimator.GetComponent<SpriteRenderer>();
                string spriteName = headSR != null && headSR.sprite != null ? headSR.sprite.name : "NULL";
                
                Debug.Log($"[{playerType}] HEAD sprite={spriteName}, " +
                          $"Speed={headAnimator.GetFloat("Speed"):F2}, " +
                          $"yVelocity={headAnimator.GetFloat("yVelocity"):F2}, " +
                          $"IsGrounded={headAnimator.GetBool("IsGrounded")}, " +
                          $"stateHash={stateInfo.shortNameHash}");
            }
        }

        // Flip sprite direction
        if (moveInput > 0f)
            transform.localScale = new Vector3(1f, 1f, 1f);
        else if (moveInput < 0f)
            transform.localScale = new Vector3(-1f, 1f, 1f);

        // Jump: ONLY when grounded AND not moving upward
        if (jumpPressed && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            isGrounded = false;
        }
    }

    private void ApplyMovement(float moveInput)
    {
        bool onSlope = isGrounded && groundNormal.y < 0.99f && groundNormal.y > 0f;

        if (!Mathf.Approximately(moveInput, 0f))
        {
            if (onSlope)
            {
                Vector2 moveDir = new Vector2(moveInput, 0f);
                Vector2 slopeMove = Vector3.ProjectOnPlane(moveDir, groundNormal).normalized * moveSpeed;
                float targetY = (slopeMove.y > 0f) ? slopeMove.y : rb.linearVelocity.y;
                rb.linearVelocity = new Vector2(slopeMove.x, targetY);
            }
            else
            {
                rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
            }
        }
        else if (isGrounded)
        {
            if (!onSlope)
            {
                // Brake on flat ground only
                float brakingX = Mathf.MoveTowards(rb.linearVelocity.x, 0f, moveSpeed * 8f * Time.deltaTime);
                rb.linearVelocity = new Vector2(brakingX, rb.linearVelocity.y);
            }
            // If on a slope and no input, do nothing.
            // This allows gravity and 0 friction to slide the player down naturally.
        }
    }

    private void UpdateGroundState()
    {
        // 1. VELOCITY-BASED DOUBLE JUMP PREVENTION
        // If upward velocity exceeds normal slope walking speeds (e.g. > 5.6), 
        // the player MUST have jumped. Force isGrounded = false.
        if (rb.linearVelocity.y > moveSpeed * 0.8f)
        {
            isGrounded = false;
            groundNormal = Vector2.up;
            return;
        }

        groundNormal = Vector2.up;
        bool foundGround = false;

        // 2. CHECK PHYSICS CONTACTS
        int count = rb.GetContacts(contacts);
        for (int i = 0; i < count; i++)
        {
            if (contacts[i].normal.y > 0.5f) // Threshold 0.5 allows up to 60-degree slopes
            {
                foundGround = true;
                groundNormal = contacts[i].normal;
                break;
            }
        }

        // 3. OVERLAPCIRCLE FALLBACK (Crucial Fix)
        // If we still didn't find ground, use the circle check.
        if (!foundGround && groundCheck != null)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(groundCheck.position, groundCheckRadius, groundLayer);
            foreach (Collider2D hit in hits)
            {
                // BUG FIX: Ignore the player's OWN collider!
                // Previously, the circle was detecting the player's CapsuleCollider 
                // causing them to ALWAYS be "grounded" even in mid-air.
                if (hit.gameObject != gameObject && !hit.transform.IsChildOf(transform))
                {
                    foundGround = true;
                    groundNormal = Vector2.up;
                    break;
                }
            }
        }

        isGrounded = foundGround;
    }

    private float GetHorizontalInput()
    {
        float input = 0f;

        // 1. New Input System
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (playerType == PlayerType.Fireboy)
            {
                if (kb.leftArrowKey.isPressed) input = -1f;
                else if (kb.rightArrowKey.isPressed) input = 1f;
            }
            else
            {
                if (kb.aKey.isPressed) input = -1f;
                else if (kb.dKey.isPressed) input = 1f;
            }
        }

        // 2. Legacy Input System fallback
        if (Mathf.Approximately(input, 0f))
        {
            if (playerType == PlayerType.Fireboy)
            {
                if (Input.GetKey(KeyCode.LeftArrow)) input = -1f;
                else if (Input.GetKey(KeyCode.RightArrow)) input = 1f;
            }
            else
            {
                if (Input.GetKey(KeyCode.A)) input = -1f;
                else if (Input.GetKey(KeyCode.D)) input = 1f;
            }
        }

        return input;
    }

    private bool GetJumpInput()
    {
        // 1. New Input System
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (playerType == PlayerType.Fireboy && kb.upArrowKey.wasPressedThisFrame) return true;
            if (playerType == PlayerType.Watergirl && kb.wKey.wasPressedThisFrame) return true;
        }

        // 2. Legacy fallback
        if (playerType == PlayerType.Fireboy && Input.GetKeyDown(KeyCode.UpArrow)) return true;
        if (playerType == PlayerType.Watergirl && Input.GetKeyDown(KeyCode.W)) return true;
        // 2. Fallback to legacy Input System
        if (playerType == PlayerType.Fireboy)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow)) return true;
        }
        else // Watergirl
        {
            if (Input.GetKeyDown(KeyCode.W)) return true;
        }

        return false;
    }

    private bool CheckGrounded()
    {
        if (groundCheck == null) return false;

        // Use OverlapCircleAll to find everything overlapping the groundCheck.
        // We loop through the results to find a valid ground collider,
        // specifically ignoring our own colliders (the parent and any children).
        Collider2D[] colliders = Physics2D.OverlapCircleAll(groundCheck.position, groundCheckRadius, groundLayer);
        
        foreach (Collider2D col in colliders)
        {
            // If the collider is NOT this gameObject, and NOT a child (like Body_Visual/Head_Visual)
            if (col.gameObject != gameObject && !col.transform.IsChildOf(transform))
            {
                return true;
            }
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}