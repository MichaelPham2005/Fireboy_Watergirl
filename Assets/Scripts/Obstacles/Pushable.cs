using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PushableRock : MonoBehaviour
{
    [Header("Push Settings")]
    public float pushForce = 5f;
    public float horizontalDamping = 5f;
    public float maxPushSpeed = 3f;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Configure physics for a heavy object
        rb.mass = 5f;
        rb.gravityScale = 3f;
        rb.angularDamping = 2f; 

        // Comment out the line below to allow natural tilting on slopes
        // rb.constraints = RigidbodyConstraints2D.FreezeRotation; 

        // Apply friction to interact with surfaces
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && col.sharedMaterial == null)
        {
            col.sharedMaterial = new PhysicsMaterial2D("RockMaterial") { friction = 0.5f };
        }
    }

    public void ApplyPush(float direction)
    {
        // Apply force only if below speed limit
        if (Mathf.Abs(rb.linearVelocity.x) < maxPushSpeed)
        {
            rb.AddForce(Vector2.right * direction * pushForce, ForceMode2D.Force);
        }
    }

    void FixedUpdate()
    {
        // Apply damping to stop sliding
        if (Mathf.Abs(rb.linearVelocity.x) > 0.1f)
        {
            rb.linearVelocity = new Vector2(
                Mathf.MoveTowards(rb.linearVelocity.x, 0f, horizontalDamping * Time.fixedDeltaTime),
                rb.linearVelocity.y
            );
        }
    }
}