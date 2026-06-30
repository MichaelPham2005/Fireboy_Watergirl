using UnityEngine;

public class StandardPlayerMovement : MonoBehaviour
{
    public enum PlayerType { Fireboy, Watergirl }

    [Header("Player Settings")]
    public PlayerType playerType = PlayerType.Fireboy;

    [Header("Movement Settings")]
    public float moveSpeed = 7f;
    public float jumpForce = 12f;

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

        // If groundLayer is set to "Nothing" (0 in int terms), default to "Default"
        if (((int)groundLayer) == 0)
        {
            groundLayer = LayerMask.GetMask("Default");
        }

        // --- DEBUG: Check animator references ---
        Debug.Log($"[{playerType}] bodyAnimator is {(bodyAnimator != null ? "ASSIGNED (" + bodyAnimator.gameObject.name + ")" : "NULL")}");
        Debug.Log($"[{playerType}] headAnimator is {(headAnimator != null ? "ASSIGNED (" + headAnimator.gameObject.name + ")" : "NULL")}");
        
        if (headAnimator != null)
        {
            var ctrl = headAnimator.runtimeAnimatorController;
            Debug.Log($"[{playerType}] headAnimator controller: {(ctrl != null ? ctrl.name : "NULL")}");
            Debug.Log($"[{playerType}] headAnimator enabled: {headAnimator.enabled}");
            Debug.Log($"[{playerType}] headAnimator gameObject active: {headAnimator.gameObject.activeInHierarchy}");
            
            // Check if the Animator has the expected parameters
            foreach (var param in headAnimator.parameters)
            {
                Debug.Log($"[{playerType}] headAnimator parameter: {param.name} (type: {param.type})");
            }
        }
    }

    void Update()
    {
        // Check ground state with multiple fallbacks
        isGrounded = CheckGrounded();

        // Get movement and jump inputs based on the selected character
        float moveInput = GetHorizontalInput();
        bool jumpPressed = GetJumpInput();

        // Apply velocity
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        // --- ANIMATION & HEAD TILT LOGIC ---
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
            if (!isGrounded)
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