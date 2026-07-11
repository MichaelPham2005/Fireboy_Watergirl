using UnityEngine;
using Fusion;
using Fusion.Sockets;
using Network;

public class StandardPlayerMovement : NetworkBehaviour
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

    // --- State ---
    private bool isGroundedLocal;
    private Vector2 groundNormal = Vector2.up;
    private ContactPoint2D[] contacts = new ContactPoint2D[8];

    // --- Networked State ---
    [Networked] public NetworkBool IsGrounded { get; set; }
    [Networked] public float CurrentSpeed { get; set; }
    [Networked] public float CurrentYVelocity { get; set; }
    [Networked] public float FacingDirection { get; set; }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        rb.gravityScale = 3f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (((int)groundLayer) == 0)
            groundLayer = LayerMask.GetMask("Default");

        PhysicsMaterial2D noFriction = new PhysicsMaterial2D("Player_NoFriction")
        {
            friction = 0f,
            bounciness = 0f
        };
        if (col != null) col.sharedMaterial = noFriction;

        transform.localScale = new Vector3(1f, 1f, 1f);
    }

    // --- LOCAL CO-OP LOGIC ---
    private void Update()
    {
        if (GameModeManager.CurrentMode == GameModeManager.GameMode.LocalCoop)
        {
            UpdateGroundStateLocal();

            float moveInput = GetHorizontalInputLocal();
            bool jumpPressed = GetJumpInputLocal();

            ApplyMovementLocal(moveInput, jumpPressed);
            UpdateAnimations(Mathf.Abs(moveInput), rb.linearVelocity.y, isGroundedLocal, moveInput > 0 ? 1f : (moveInput < 0 ? -1f : 0f));
        }
    }

    private float GetHorizontalInputLocal()
    {
        float input = 0f;
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
        else
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

    private bool GetJumpInputLocal()
    {
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (playerType == PlayerType.Fireboy && kb.upArrowKey.wasPressedThisFrame) return true;
            if (playerType == PlayerType.Watergirl && kb.wKey.wasPressedThisFrame) return true;
        }

        if (playerType == PlayerType.Fireboy && Input.GetKeyDown(KeyCode.UpArrow)) return true;
        if (playerType == PlayerType.Watergirl && Input.GetKeyDown(KeyCode.W)) return true;

        return false;
    }

    private void ApplyMovementLocal(float moveInput, bool jumpPressed)
    {
        bool onSlope = isGroundedLocal && groundNormal.y < 0.99f && groundNormal.y > 0f;

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
        else if (isGroundedLocal)
        {
            if (!onSlope)
            {
                float brakingX = Mathf.MoveTowards(rb.linearVelocity.x, 0f, moveSpeed * 8f * Time.deltaTime);
                rb.linearVelocity = new Vector2(brakingX, rb.linearVelocity.y);
            }
        }

        if (jumpPressed && isGroundedLocal)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            isGroundedLocal = false;
        }
    }

    private void UpdateGroundStateLocal()
    {
        if (rb.linearVelocity.y > moveSpeed * 0.8f)
        {
            isGroundedLocal = false;
            groundNormal = Vector2.up;
            return;
        }

        groundNormal = Vector2.up;
        bool foundGround = false;

        int count = rb.GetContacts(contacts);
        for (int i = 0; i < count; i++)
        {
            if (contacts[i].normal.y > 0.5f)
            {
                foundGround = true;
                groundNormal = contacts[i].normal;
                break;
            }
        }

        if (!foundGround && groundCheck != null)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(groundCheck.position, groundCheckRadius, groundLayer);
            foreach (Collider2D hit in hits)
            {
                if (hit.gameObject != gameObject && !hit.transform.IsChildOf(transform))
                {
                    foundGround = true;
                    groundNormal = Vector2.up;
                    break;
                }
            }
        }

        isGroundedLocal = foundGround;
    }


    // --- ONLINE MULTIPLAYER LOGIC (FUSION) ---

    public override void Spawned()
    {
        if (GameModeManager.CurrentMode == GameModeManager.GameMode.OnlineMultiplayer)
        {
            // In a pure Server-Authoritative setup without physics prediction, 
            // the proxy clients should not run local physics.
            if (!HasStateAuthority && rb != null)
            {
                rb.isKinematic = true;
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (GameModeManager.CurrentMode != GameModeManager.GameMode.OnlineMultiplayer) return;

        // Only the Host (State Authority) runs the actual physics simulation.
        if (!HasStateAuthority) return;

        if (GetInput(out PlayerInputData data))
        {
            UpdateGroundStateLocal(); // Use local physics for grounding

            bool jumpPressed = data.JumpPressed;
            ApplyMovementNetwork(data.Horizontal, jumpPressed);

            // Sync state for remote players to render properly
            IsGrounded = isGroundedLocal;
            CurrentSpeed = Mathf.Abs(data.Horizontal);
            CurrentYVelocity = rb.linearVelocity.y;
            
            if (data.Horizontal > 0) FacingDirection = 1f;
            else if (data.Horizontal < 0) FacingDirection = -1f;
        }
    }

    public override void Render()
    {
        if (GameModeManager.CurrentMode == GameModeManager.GameMode.OnlineMultiplayer)
        {
            UpdateAnimations(CurrentSpeed, CurrentYVelocity, IsGrounded, FacingDirection);

            // Trigger camera setup once if we are the local player
            if (HasInputAuthority && Camera.main != null)
            {
                var camFollow = Camera.main.GetComponent<NetworkCameraFollow>();
                if (camFollow != null && camFollow.target != transform)
                {
                    camFollow.SetTarget(transform);
                }
            }
        }
    }

    private void ApplyMovementNetwork(float moveInput, bool jumpPressed)
    {
        bool onSlope = isGroundedLocal && groundNormal.y < 0.99f && groundNormal.y > 0f;

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
        else if (isGroundedLocal)
        {
            if (!onSlope)
            {
                float brakingX = Mathf.MoveTowards(rb.linearVelocity.x, 0f, moveSpeed * 8f * Runner.DeltaTime);
                rb.linearVelocity = new Vector2(brakingX, rb.linearVelocity.y);
            }
        }

        if (jumpPressed && isGroundedLocal)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            isGroundedLocal = false;
        }
    }


    // --- SHARED LOGIC ---

    private void UpdateAnimations(float speed, float yVel, bool grounded, float direction)
    {
        if (bodyAnimator != null)
        {
            bodyAnimator.SetFloat("Speed", speed);
            bodyAnimator.SetFloat("yVelocity", yVel);
            bodyAnimator.SetBool("IsGrounded", grounded);
        }
        
        if (headAnimator != null)
        {
            headAnimator.SetFloat("Speed", speed);
            headAnimator.SetFloat("yVelocity", yVel);
            headAnimator.SetBool("IsGrounded", grounded);

            float targetZRotation = 0f;
            if (!grounded && speed > 0.1f)
            {
                targetZRotation = Mathf.Clamp(yVel * 2.5f, -35f, 35f);
            }
            
            Quaternion targetRot = Quaternion.Euler(0, 0, targetZRotation);
            headAnimator.transform.localRotation = Quaternion.Lerp(
                headAnimator.transform.localRotation, 
                targetRot, 
                Time.deltaTime * 12f
            );
        }

        if (direction > 0f)
            transform.localScale = new Vector3(1f, 1f, 1f);
        else if (direction < 0f)
            transform.localScale = new Vector3(-1f, 1f, 1f);
    }

    public void FreezeForWin()
    {
        rb.linearVelocity = Vector2.zero;
        if (bodyAnimator != null) bodyAnimator.SetFloat("Speed", 0f);
        if (headAnimator != null) headAnimator.SetFloat("Speed", 0f);
        this.enabled = false;
    }

    public void TriggerWinSequence()
    {
        rb.linearVelocity = Vector2.zero;
        if (bodyAnimator != null) bodyAnimator.SetTrigger("EnterDoor");
        if (headAnimator != null)
        {
            SpriteRenderer headSprite = headAnimator.GetComponent<SpriteRenderer>();
            if (headSprite != null) headSprite.enabled = false;
        }
        this.enabled = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }

    // End of class
}