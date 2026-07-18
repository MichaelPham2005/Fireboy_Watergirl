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
    private float jumpTimer = 0f;

    // --- Networked State ---
    [Networked] public NetworkBool IsGrounded { get; set; }
    [Networked] public float CurrentSpeed { get; set; }
    [Networked] public float CurrentYVelocity { get; set; }
    [Networked] public float FacingDirection { get; set; }

    // Footstep timing
    private float footstepTimer = 0f;
    private const float FootstepInterval = 0.35f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        if (rb == null)
        {
            Debug.Log("No Rigidbody2D found. Disabling StandardPlayerMovement script for preview mode.");
            enabled = false;
            return;
        }

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

        LoadEquippedAccessory();
    }

    private void LoadEquippedAccessory()
    {
        if (playerType == PlayerType.Fireboy)
        {
            int equippedTie = PlayerPrefs.GetInt("FB_Tie", -1);
            if (equippedTie < 0) return;

            Sprite tieSprite = Resources.Load<Sprite>("tie");
            if (tieSprite == null)
            {
                Debug.LogWarning("Tie sprite not found in Resources!");
                return;
            }

            GameObject tieGo = new GameObject("Equipped_Tie");
            if (bodyAnimator != null)
                tieGo.transform.SetParent(bodyAnimator.transform, false);
            else
                tieGo.transform.SetParent(transform, false);

            SpriteRenderer sr = tieGo.AddComponent<SpriteRenderer>();
            sr.sprite = tieSprite;

            SpriteRenderer bodySr = bodyAnimator != null ? bodyAnimator.GetComponent<SpriteRenderer>() : GetComponent<SpriteRenderer>();
            if (bodySr != null)
            {
                sr.sortingLayerID = bodySr.sortingLayerID;
                sr.sortingOrder = bodySr.sortingOrder + 1;
            }
            else
            {
                sr.sortingOrder = 10;
            }

            Color tieColor = Color.white;
            switch (equippedTie)
            {
                case 0: tieColor = Color.white; break;
                case 1: tieColor = new Color(0f, 0f, 0.867f, 1f); break; // Blue
                case 2: tieColor = new Color(1f, 0f, 0.708f, 1f); break; // Pink
                case 3: tieColor = new Color(0f, 0.83f, 0.199f, 1f); break; // Green
            }
            sr.color = tieColor;

            tieGo.transform.localPosition = new Vector3(0.003f, -0.32f, 0f);
            tieGo.transform.localScale = new Vector3(1.3f, 0.9f, 1f);
        }
        else if (playerType == PlayerType.Watergirl)
        {
            int equippedBowtie = PlayerPrefs.GetInt("WG_Tie", -1);
            if (equippedBowtie < 0) return;

            Sprite bowtieSprite = Resources.Load<Sprite>("scarf");
            if (bowtieSprite == null)
            {
                Debug.LogWarning("Scarf sprite not found in Resources!");
                return;
            }

            GameObject bowtieGo = new GameObject("Equipped_Scarf");
            if (bodyAnimator != null)
                bowtieGo.transform.SetParent(bodyAnimator.transform, false);
            else
                bowtieGo.transform.SetParent(transform, false);

            SpriteRenderer sr = bowtieGo.AddComponent<SpriteRenderer>();
            sr.sprite = bowtieSprite;

            SpriteRenderer bodySr = bodyAnimator != null ? bodyAnimator.GetComponent<SpriteRenderer>() : GetComponent<SpriteRenderer>();
            if (bodySr != null)
            {
                sr.sortingLayerID = bodySr.sortingLayerID;
                sr.sortingOrder = bodySr.sortingOrder + 1;
            }
            else
            {
                sr.sortingOrder = 10;
            }

            Color bowtieColor = Color.white;
            switch (equippedBowtie)
            {
                case 0: bowtieColor = Color.white; break;
                case 1: bowtieColor = new Color(0f, 0f, 0.867f, 1f); break; // Blue
                case 2: bowtieColor = new Color(1f, 0f, 0.708f, 1f); break; // Pink
                case 3: bowtieColor = new Color(0f, 0.83f, 0.199f, 1f); break; // Green
            }
            sr.color = bowtieColor;

            bowtieGo.transform.localPosition = new Vector3(0.003f, -0.2f, 0f);
            bowtieGo.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
        }
    }

    // --- LOCAL CO-OP LOGIC ---
    private void Update()
    {
        if (jumpTimer > 0f) jumpTimer -= Time.deltaTime;

        if (GameModeManager.CurrentMode == GameModeManager.GameMode.LocalCoop)
        {
            UpdateGroundStateLocal();

            float moveInput = GetHorizontalInputLocal();
            bool jumpPressed = GetJumpInputLocal();

            ApplyMovementLocal(moveInput, jumpPressed);
            UpdateAnimations(Mathf.Abs(moveInput), rb.linearVelocity.y, isGroundedLocal, moveInput > 0 ? 1f : (moveInput < 0 ? -1f : 0f));

            if (isGroundedLocal && Mathf.Abs(rb.linearVelocity.x) > 1f)
            {
                footstepTimer -= Time.deltaTime;
                if (footstepTimer <= 0f)
                {
                    AudioManager.Instance?.PlaySteps();
                    footstepTimer = FootstepInterval;
                }
            }
            else
            {
                footstepTimer = 0f;
            }

            if (jumpPressed && isGroundedLocal)
            {
                AudioManager.Instance?.PlayJump(playerType == PlayerType.Fireboy);
            }
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
                // We are on flat ground or airborne.
                float targetY = rb.linearVelocity.y;
                if (isGrounded)
                {
                    // If grounded on flat ground, kill upward velocity to prevent flying off slopes.
                    targetY = Mathf.Min(targetY, 0f);
                }
                rb.linearVelocity = new Vector2(moveInput * moveSpeed, targetY);
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
            jumpTimer = 0.15f;
        }
    }

    private void UpdateGroundStateLocal()
    {
        // 1. JUMP TIMER CHECK
        // Safely prevents double-jumping and gives the player time to leave the ground
        // without erroneously un-grounding them during slope-collision physics bounces.
        if (jumpTimer > 0f)
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

    public float GetHorizontalInput()
    {
        // If we are in multiplayer and remote, we might not have local input.
        // For now, return the networked CurrentSpeed & FacingDirection if it's not local co-op.
        if (GameModeManager.CurrentMode == GameModeManager.GameMode.OnlineMultiplayer)
        {
            return CurrentSpeed * FacingDirection;
        }
        return GetHorizontalInputLocal();
    }

    // --- ONLINE MULTIPLAYER LOGIC (FUSION) ---

    public override void Spawned()
    {
        // NetworkRigidbody2D handles proxy kinematics automatically now.
    }

    public override void FixedUpdateNetwork()
    {
        if (GameModeManager.CurrentMode != GameModeManager.GameMode.OnlineMultiplayer) return;

        // Only the Host (State Authority) runs the actual physics simulation.
        if (!HasStateAuthority) return;

        if (jumpTimer > 0f) jumpTimer -= Runner.DeltaTime;
        
        // --- AUTO-ASSIGN INPUT AUTHORITY FOR SCENE OBJECTS ---
        if (Object.InputAuthority == PlayerRef.None)
        {
            if (playerType == PlayerType.Fireboy)
            {
                Object.AssignInputAuthority(Runner.LocalPlayer);
                Debug.Log($"Auto-assigned Fireboy to Host {Runner.LocalPlayer.PlayerId}");
            }
            else if (playerType == PlayerType.Watergirl)
            {
                // Find the first client to assign Watergirl to
                foreach (var p in Runner.ActivePlayers)
                {
                    if (p != Runner.LocalPlayer)
                    {
                        Object.AssignInputAuthority(p);
                        Debug.Log($"Auto-assigned Watergirl to Client {p.PlayerId}");
                        break;
                    }
                }
            }
        }

        bool gotInput = GetInput(out PlayerInputData data);

        if (gotInput)
        {
            if (data.Horizontal != 0) Debug.Log($"Moving {gameObject.name} with input: {data.Horizontal}");
            
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

            // Network Footsteps
            if (IsGrounded && CurrentSpeed > 0.1f)
            {
                footstepTimer -= Time.deltaTime;
                if (footstepTimer <= 0f)
                {
                    AudioManager.Instance?.PlaySteps();
                    footstepTimer = FootstepInterval;
                }
            }
            else
            {
                footstepTimer = 0f;
            }

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
            jumpTimer = 0.15f;
            AudioManager.Instance?.PlayJump(playerType == PlayerType.Fireboy);
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
        // Hide accessories like tie and scarf so they don't show on the player's back
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);
        foreach (var child in allChildren)
        {
            if (child.gameObject.name == "Equipped_Tie" || child.gameObject.name == "Equipped_Scarf")
            {
                child.gameObject.SetActive(false);
            }
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