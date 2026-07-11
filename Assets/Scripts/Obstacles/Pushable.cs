using UnityEngine;
using Fusion;
using Network;

[RequireComponent(typeof(Rigidbody2D))]
public class PushableRock : NetworkBehaviour
{
    [Header("Push Settings")]
    public float pushForce = 5f;
    public float horizontalDamping = 5f;
    public float maxPushSpeed = 3f;

    private Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        rb.mass = 5f;
        rb.gravityScale = 3f;
        rb.angularDamping = 2f; 

        Collider2D col = GetComponent<Collider2D>();
        if (col != null && col.sharedMaterial == null)
        {
            col.sharedMaterial = new PhysicsMaterial2D("RockMaterial") { friction = 0.5f };
        }
    }

    public void ApplyPush(float direction)
    {
        // Only host or local co-op processes explicit pushes
        if (GameModeManager.CurrentMode == GameModeManager.GameMode.OnlineMultiplayer && !HasStateAuthority)
            return;

        if (Mathf.Abs(rb.linearVelocity.x) < maxPushSpeed)
        {
            rb.AddForce(Vector2.right * direction * pushForce, ForceMode2D.Force);
        }
    }

    private void FixedUpdate()
    {
        if (GameModeManager.CurrentMode == GameModeManager.GameMode.LocalCoop)
        {
            ProcessDamping(Time.fixedDeltaTime);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (GameModeManager.CurrentMode == GameModeManager.GameMode.OnlineMultiplayer && HasStateAuthority)
        {
            ProcessDamping(Runner.DeltaTime);
        }
    }

    private void ProcessDamping(float deltaTime)
    {
        if (Mathf.Abs(rb.linearVelocity.x) > 0.1f)
        {
            rb.linearVelocity = new Vector2(
                Mathf.MoveTowards(rb.linearVelocity.x, 0f, horizontalDamping * deltaTime),
                rb.linearVelocity.y
            );
        }
    }
}