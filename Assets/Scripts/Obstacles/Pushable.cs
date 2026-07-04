using UnityEngine;

/// <summary>
/// Attach to any pushable object (e.g. Rock).
/// Ensures the object can only be pushed horizontally by the player
/// and does not get launched vertically.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Pushable : MonoBehaviour
{
    [Header("Push Settings")]
    [Tooltip("Horizontal damping when nothing is pushing the rock. Higher = stops faster.")]
    public float horizontalDamping = 10f;

    [Tooltip("Maximum horizontal speed the rock can be pushed to.")]
    public float maxPushSpeed = 5f;

    private Rigidbody2D rb;
    private ContactPoint2D[] contacts = new ContactPoint2D[4];

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // Freeze rotation so the rock never tips over
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Apply zero friction so the rock doesn't get stuck in corners or on walls
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            PhysicsMaterial2D noFriction = new PhysicsMaterial2D("Rock_NoFriction")
            {
                friction = 0f,
                bounciness = 0f
            };
            col.sharedMaterial = noFriction;
        }
    }

    void FixedUpdate()
    {
        bool isGrounded = false;
        int count = rb.GetContacts(contacts);
        for (int i = 0; i < count; i++)
        {
            // Check if there is ground below the rock
            if (contacts[i].normal.y > 0.5f)
            {
                isGrounded = true;
                break;
            }
        }

        float targetX = rb.linearVelocity.x;

        if (!isGrounded)
        {
            // 1. FALL STRAIGHT DOWN
            // If the rock is in the air, instantly kill horizontal velocity
            targetX = 0f;
        }
        else
        {
            // 2. STOP WHEN NOT PUSHED
            // On the ground, apply damping so it stops sliding when the player lets go
            targetX = Mathf.MoveTowards(rb.linearVelocity.x, 0f, horizontalDamping * Time.fixedDeltaTime);
        }

        // Clamp horizontal velocity so the rock can't be infinitely accelerated by the player
        float clampedX = Mathf.Clamp(targetX, -maxPushSpeed, maxPushSpeed);
        
        rb.linearVelocity = new Vector2(clampedX, rb.linearVelocity.y);
    }
}
