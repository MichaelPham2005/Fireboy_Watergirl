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

    // Footstep timing
    private float footstepTimer = 0f;
    private const float FootstepInterval = 0.35f;

    void Start()
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

        // Load character customizations for gameplay
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
            AudioManager.Instance?.PlayJump(playerType == PlayerType.Fireboy);
        }

        // Footsteps: play a step sound periodically while running on the ground
        if (isGrounded && Mathf.Abs(rb.linearVelocity.x) > 1f)
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
            footstepTimer = 0f; // Reset so next step plays immediately
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

    public void FreezeForWin()
    {
        // Stop physical movement
        rb.linearVelocity = Vector2.zero;
        
        // Reset animator parameters so they don't get stuck running in place
        if (bodyAnimator != null) bodyAnimator.SetFloat("Speed", 0f);
        if (headAnimator != null) headAnimator.SetFloat("Speed", 0f);
        
        // Disable script to stop input
        this.enabled = false;
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

    public void TriggerWinSequence()
    {
        rb.linearVelocity = Vector2.zero;
        
        // Play the full-body animation on the Body animator
        if (bodyAnimator != null) bodyAnimator.SetTrigger("EnterDoor");
        
        // Hide the head completely so it doesn't overlap the full-body animation
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
        
        // Stop accepting input
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
}