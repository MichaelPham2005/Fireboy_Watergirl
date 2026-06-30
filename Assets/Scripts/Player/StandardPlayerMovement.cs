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
    public Animator bodyAnimator; 
    public Animator headAnimator;

    [Header("Ground Detection")]
    public Transform groundCheck; 
    public float groundCheckRadius = 0.25f;
    public LayerMask groundLayer; 
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (((int)groundLayer) == 0)
        {
            groundLayer = LayerMask.GetMask("Default");
        }
    }

    void Update()
    {
        // Check ground state with multiple fallbacks
        isGrounded = CheckGrounded();

        // Get movement and jump inputs based on the selected character
        float moveInput = GetHorizontalInput();
        bool jumpPressed = GetJumpInput();

        // Apply horizontal velocity
        // When there's input: override velocity directly
        // When no input AND on ground: stop (don't slide on flat floor)
        // When no input AND in air: let physics handle x (allows sliding down slopes)
        if (!Mathf.Approximately(moveInput, 0f))
        {
            rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        }
        else if (isGrounded)
        {
            // On ground with no input — brake to stop (works on flat ground)
            // Slopes will still cause slight sliding via physics if friction is 0
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.5f, rb.linearVelocity.y);
        }
        // If airborne with no input, do nothing — let gravity + physics handle it

        // --- ANIMATION LOGIC ---
        if (bodyAnimator != null)
        {
            bodyAnimator.SetFloat("Speed", Mathf.Abs(moveInput));
        }
        if (headAnimator != null)
        {
            headAnimator.SetFloat("Speed", Mathf.Abs(moveInput));
        }

        // Flip the character sprite based on movement direction
        if (moveInput > 0)
        {
            transform.localScale = new Vector3(1, 1, 1); // Face Right
        }
        else if (moveInput < 0)
        {
            transform.localScale = new Vector3(-1, 1, 1); // Face Left
        }

        // Apply jump force
        if (jumpPressed && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    private float GetHorizontalInput()
    {
        float input = 0f;

        // 1. Try New Input System
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (playerType == PlayerType.Fireboy)
            {
                if (keyboard.leftArrowKey.isPressed) input = -1f;
                else if (keyboard.rightArrowKey.isPressed) input = 1f;
            }
            else // Watergirl
            {
                if (keyboard.aKey.isPressed) input = -1f;
                else if (keyboard.dKey.isPressed) input = 1f;
            }
        }
        
        // 2. Fallback to legacy Input System if no input was captured
        if (Mathf.Approximately(input, 0f))
        {
            if (playerType == PlayerType.Fireboy)
            {
                if (Input.GetKey(KeyCode.LeftArrow)) input = -1f;
                else if (Input.GetKey(KeyCode.RightArrow)) input = 1f;
            }
            else // Watergirl
            {
                if (Input.GetKey(KeyCode.A)) input = -1f;
                else if (Input.GetKey(KeyCode.D)) input = 1f;
            }
        }

        return input;
    }

    private bool GetJumpInput()
    {
        // 1. Try New Input System
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (playerType == PlayerType.Fireboy)
            {
                if (keyboard.upArrowKey.wasPressedThisFrame) return true;
            }
            else // Watergirl
            {
                if (keyboard.wKey.wasPressedThisFrame) return true;
            }
        }

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
        // CRITICAL: If moving upward, we are NOT grounded — this prevents double jump
        if (rb.linearVelocity.y > 0.5f) return false;

        // Method A: contact normals (works regardless of layer mask)
        ContactPoint2D[] contacts = new ContactPoint2D[8];
        int count = rb.GetContacts(contacts);
        for (int i = 0; i < count; i++)
        {
            if (contacts[i].normal.y > 0.6f)
            {
                return true;
            }
        }

        // Method B: OverlapCircle fallback
        if (groundCheck != null && Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer))
        {
            return true;
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