using UnityEngine;

public class StandardPlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 7f;
    public float jumpForce = 12f;

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

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 3f; 
    }

    void Update()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        float moveInput = Input.GetAxis("Horizontal");
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        // --- NEW ANIMATION LOGIC ---

        // 2. Tell the Animator how fast we are moving (Mathf.Abs turns negative speeds positive)
        if (bodyAnimator != null)
        {
            bodyAnimator.SetFloat("Speed", Mathf.Abs(moveInput));
        }
        if (headAnimator != null)
        {
            headAnimator.SetFloat("Speed", Mathf.Abs(moveInput));
        }

        // 3. Flip the entire character so head and body face the direction we walk
        if (moveInput > 0)
        {
            transform.localScale = new Vector3(1, 1, 1); // Face Right
        }
        else if (moveInput < 0)
        {
            transform.localScale = new Vector3(-1, 1, 1); // Face Left
        }

        // --- END NEW LOGIC ---

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
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