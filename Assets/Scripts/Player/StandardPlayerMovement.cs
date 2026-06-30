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
            bodyAnimator.SetFloat("Speed", Mathf.Abs(moveInput));
        if (headAnimator != null)
            headAnimator.SetFloat("Speed", Mathf.Abs(moveInput));

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
        if (!Mathf.Approximately(moveInput, 0f))
        {
            if (isGrounded && groundNormal != Vector2.up)
            {
                // On a slope: project movement direction along the slope surface
                // This allows walking UP and DOWN slopes smoothly
                Vector2 moveDir = new Vector2(moveInput, 0f);
                Vector2 slopeMove = Vector3.ProjectOnPlane(moveDir, groundNormal).normalized * moveSpeed;
                // Only override y if moving upward along slope (not fighting gravity when going down)
                float targetY = (slopeMove.y > 0) ? slopeMove.y : rb.linearVelocity.y;
                rb.linearVelocity = new Vector2(slopeMove.x, targetY);
            }
            else
            {
                // Flat ground or air: simple horizontal override
                rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
            }
        }
        else if (isGrounded)
        {
            // No input while grounded: brake to a stop
            // Use Lerp for smooth deceleration (not instant stop, not sticky)
            float brakingX = Mathf.MoveTowards(rb.linearVelocity.x, 0f, moveSpeed * 8f * Time.deltaTime);
            rb.linearVelocity = new Vector2(brakingX, rb.linearVelocity.y);
        }
        // Airborne with no input: let physics handle everything (enables slope sliding)
    }

    private void UpdateGroundState()
    {
        // RULE 1: If moving upward significantly, we are NOT grounded.
        // This is the primary double-jump prevention mechanism.
        if (rb.linearVelocity.y > 0.0f)
        {
            isGrounded = false;
            groundNormal = Vector2.up;
            return;
        }

        // RULE 2: Check contact normals.
        // Threshold 0.75 means the surface must be within ~41 degrees of horizontal.
        // A pure vertical wall has normal.y = 0, so it will NEVER pass this check.
        // This prevents wall-jumping / wall-climbing.
        groundNormal = Vector2.up;
        int count = rb.GetContacts(contacts);
        for (int i = 0; i < count; i++)
        {
            if (contacts[i].normal.y > 0.75f)
            {
                isGrounded = true;
                groundNormal = contacts[i].normal;
                return;
            }
        }

        // RULE 3: OverlapCircle as a safety fallback
        if (groundCheck != null && Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer))
        {
            isGrounded = true;
            return;
        }

        isGrounded = false;
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