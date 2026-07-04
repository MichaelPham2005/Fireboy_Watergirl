using UnityEngine;

public enum PlayerType
{
    Fireboy  = 0,
    Watergirl = 1
}

public class StandardPlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 7f;
    public float jumpForce = 16f;
    public float acceleration = 50f;
    public float deceleration = 60f;

    [Header("Components")]
    private Rigidbody2D rb;
    // 1. Add a reference to the Animator
    public Animator bodyAnimator; 
    public Animator headAnimator;

    [Header("Ground Detection")]
    public Transform groundCheck; 
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer; 
    private bool isGrounded;
    public bool isTouchingLever = false; // Theo dõi xem có đang kẹt trong cần gạt không

    [Header("Player Identity")]
    public PlayerType playerType = PlayerType.Fireboy;

    // Internal: track the surface normal of what we're standing on
    private Vector2 groundNormal = Vector2.up;
    private ContactPoint2D[] contacts = new ContactPoint2D[8];
    private Vector3 initialScale;
    private Collider2D col;
    private bool jumpPressed;

    void Start()
    {
        initialScale = transform.localScale;
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        rb.gravityScale = 3f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Fallback: if no ground layer assigned in Inspector, use Default
        if (((int)groundLayer) == 0)
            groundLayer = LayerMask.GetMask("Default");

        PhysicsMaterial2D noFriction = new PhysicsMaterial2D("Player_NoFriction")
        {
            friction = 0f,
            bounciness = 0f
        };
        if (col != null)
            col.sharedMaterial = noFriction;
    }

    void FixedUpdate()
    {
        // ANTI-FLY HACK: Chạy đồng bộ với Physics Engine. 
        // Nếu physics engine cố tình ép nhân vật trượt lên dốc cần gạt, ta ép nó xuống lại ngay lập tức!
        if (isTouchingLever && isGrounded && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -2f);
        }
    }

    void Update()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        float moveInput = Input.GetAxis("Horizontal");
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        // --- NEW ANIMATION LOGIC ---

        // 2. Tell the Animator how fast we are moving (Mathf.Abs turns negative speeds positive)
        if (bodyAnimator != null && bodyAnimator.runtimeAnimatorController != null)
        {
            bodyAnimator.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x)); 
            bodyAnimator.SetFloat("yVelocity", rb.linearVelocity.y);
            bodyAnimator.SetBool("IsGrounded", isGrounded);
        }
        if (headAnimator != null && headAnimator.runtimeAnimatorController != null)
        {
            headAnimator.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
            headAnimator.SetFloat("yVelocity", rb.linearVelocity.y);
            headAnimator.SetBool("IsGrounded", isGrounded);

            float targetZRotation = 0f;
            if (!isGrounded && Mathf.Abs(rb.linearVelocity.x) > 0.1f)
            {
                targetZRotation = Mathf.Clamp(rb.linearVelocity.y * 2.5f, -35f, 35f);
            }
            
            Quaternion targetRot = Quaternion.Euler(0, 0, targetZRotation);
            headAnimator.transform.localRotation = Quaternion.Lerp(
                headAnimator.transform.localRotation, 
                targetRot, 
                Time.deltaTime * 12f
            );
        }

        if (moveInput > 0f)
            transform.localScale = new Vector3(Mathf.Abs(initialScale.x), initialScale.y, initialScale.z);
        else if (moveInput < 0f)
            transform.localScale = new Vector3(-Mathf.Abs(initialScale.x), initialScale.y, initialScale.z);

        jumpPressed = GetJumpInput();
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
                float currentX = Mathf.MoveTowards(rb.linearVelocity.x, slopeMove.x, acceleration * Time.deltaTime);
                rb.linearVelocity = new Vector2(currentX, targetY);
            }
            else
            {
                float targetX = moveInput * moveSpeed;
                float currentX = Mathf.MoveTowards(rb.linearVelocity.x, targetX, acceleration * Time.deltaTime);
                rb.linearVelocity = new Vector2(currentX, rb.linearVelocity.y);
            }
        }
        else if (isGrounded)
        {
            if (!onSlope)
            {
                float brakingX = Mathf.MoveTowards(rb.linearVelocity.x, 0f, deceleration * Time.deltaTime);
                rb.linearVelocity = new Vector2(brakingX, rb.linearVelocity.y);
            }
        }
    }

    private void UpdateGroundState()
    {
        if (rb.linearVelocity.y > moveSpeed * 0.8f)
        {
            isGrounded = false;
            groundNormal = Vector2.up;
            return;
        }

        groundNormal = Vector2.up;
        bool foundGround = false;
        isTouchingLever = false; // Reset mỗi frame

        int count = rb.GetContacts(contacts);
        for (int i = 0; i < count; i++)
        {
            // BỎ QUA CẦN GẠT: Không coi cần gạt là mặt đất/dốc để leo lên!
            if (contacts[i].collider.GetComponent<LeverSwitch>() != null || 
                contacts[i].collider.GetComponentInParent<LeverSwitch>() != null)
            {
                isTouchingLever = true; // Ghi nhận là đang chạm cần gạt
                continue;
            }

            if (contacts[i].normal.y > 0.5f) 
            {
                foundGround = true;
                groundNormal = contacts[i].normal;
                if (groundNormal.y > 0.99f) break;
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
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}