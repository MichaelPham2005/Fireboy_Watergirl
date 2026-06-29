using UnityEngine;

public class WallWalker : MonoBehaviour
{
    private Rigidbody2D rb;
    public float moveSpeed = 5f;
    public float surfaceStickForce = 15f; 
    
    private Vector2 currentNormal = Vector2.up; // Default to standard floor

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        // Disable Unity's global gravity so we can control it ourselves
        rb.gravityScale = 0f; 
    }

    void Update()
    {
        // 1. Move left/right relative to the character's CURRENT rotation
        float moveInput = Input.GetAxis("Horizontal"); 
        transform.Translate(Vector3.right * moveInput * moveSpeed * Time.deltaTime);
    }

    void FixedUpdate()
    {
        // 2. Apply our custom gravity pulling them into the surface they are standing on
        // -transform.up is always "down" relative to the character's feet
        rb.AddForce(-transform.up * surfaceStickForce);
    }

    // 3. When we touch the Tilemap, check the angle of the surface
    void OnCollisionStay2D(Collision2D collision)
    {
        // Get the direction the wall/floor is pushing back against the player
        Vector2 contactNormal = collision.GetContact(0).normal;

        // If the surface angle changed, update the rotation
        if (contactNormal != currentNormal)
        {
            currentNormal = contactNormal;

            // Calculate the angle in degrees based on the normal vector
            float angle = Mathf.Atan2(contactNormal.y, contactNormal.x) * Mathf.Rad2Deg;
            
            // Subtract 90 degrees so the feet point toward the surface, not the head
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }
    }
}