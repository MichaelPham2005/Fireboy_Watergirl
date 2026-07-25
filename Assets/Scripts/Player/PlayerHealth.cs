using UnityEngine;
using System.Collections;
using Fusion;
using Network;

public class PlayerHealth : NetworkBehaviour
{
    [Header("Components")]
    public StandardPlayerMovement movementScript;
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    [Networked] public NetworkBool IsDead { get; set; }
    private ChangeDetector _changes;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        if (movementScript == null) movementScript = GetComponent<StandardPlayerMovement>();
    }

    public override void Spawned()
    {
        _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
        if (HasStateAuthority)
        {
            IsDead = false;
        }
    }

    public override void Render()
    {
        foreach (var change in _changes.DetectChanges(this, out var previousBuffer, out var currentBuffer))
        {
            switch (change)
            {
                case nameof(IsDead):
                    if (IsDead)
                    {
                        Die();
                    }
                    break;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        PoolElement pool = col.GetComponent<PoolElement>();
        if (pool != null) CheckDeath(pool, col);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        PoolElement pool = col.gameObject.GetComponent<PoolElement>();
        if (pool != null) CheckDeath(pool, col.collider);
    }

    void OnCollisionStay2D(Collision2D col)
    {
        PoolElement pool = col.gameObject.GetComponent<PoolElement>();
        if (pool != null) CheckDeath(pool, col.collider);
    }

    private void CheckDeath(PoolElement pool, Collider2D hazardCollider)
    {
        if (GameModeManager.CurrentMode == GameModeManager.GameMode.OnlineMultiplayer)
        {
            if (!HasStateAuthority || IsDead) return;
        }
        else
        {
            if (movementScript != null && !movementScript.enabled) return;
        }

        // 1. Check if the hazard is hitting our head instead of our feet
        if (hazardCollider != null)
        {
            Vector2 closestPoint = hazardCollider.ClosestPoint(transform.position);
            // If the closest point of the hazard is above the player's center,
            // they bumped their head from below. Ignore the hazard.
            if (closestPoint.y > transform.position.y + 0.1f)
            {
                return;
            }
        }

        bool shouldDie = false;

        // Fireboy dies in Water and Goo
        if (gameObject.CompareTag("Fireboy"))
        {
            if (pool.liquidType == LiquidType.BlueWater || pool.liquidType == LiquidType.GreenGoo)
                shouldDie = true;
        }
        // Watergirl dies in Lava and Goo
        else if (gameObject.CompareTag("Watergirl"))
        {
            if (pool.liquidType == LiquidType.RedLava || pool.liquidType == LiquidType.GreenGoo)
                shouldDie = true;
        }

        if (shouldDie)
        {
            if (GameModeManager.CurrentMode == GameModeManager.GameMode.OnlineMultiplayer)
            {
                IsDead = true;
            }
            else
            {
                Die(); 
            }
        }
    }

    public void Die()
    {
        if (movementScript != null && movementScript.enabled)
        {
            StartCoroutine(DieWithDelay(0.1f));
        }
    }

    IEnumerator DieWithDelay(float delay)
    {
        // 1. Disable movement immediately
        if (movementScript != null) movementScript.enabled = false;
        
        // Notify GameManager to trigger Game Over
        if (GameManager.Instance != null) GameManager.Instance.LoseGame();

        // 2. Trigger the Death animation on the body, and simply HIDE the head & accessories
        if (movementScript != null)
        {
            if (movementScript.bodyAnimator != null) 
                movementScript.bodyAnimator.SetTrigger("Die");
                
            if (movementScript.headAnimator != null) 
                movementScript.headAnimator.gameObject.SetActive(false); // Make head disappear instantly
        }

        // Hide accessories like tie and scarf
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);
        foreach (var child in allChildren)
        {
            if (child.gameObject.name == "Equipped_Tie" || child.gameObject.name == "Equipped_Scarf")
            {
                child.gameObject.SetActive(false);
            }
        }

        // 3. Wait for the slide delay (if you want them to slide down slopes while smoking)
        yield return new WaitForSeconds(delay);

        // 4. Stop physics simulation completely
        Debug.Log(gameObject.name + " evaporated!");
        if (rb != null) rb.simulated = false;
    }

    // Proxy RPC: Client calls this on any PlayerHealth to ask the Host to load a scene.
    // This works because PlayerHealth has a real NetworkObject that Fusion can route RPCs through.
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestSceneLoad(int buildIndex)
    {
        var controller = Network.NetworkRunnerController.Instance;
        if (controller != null && controller.Runner != null && controller.Runner.IsServer)
        {
            controller.Runner.LoadScene(SceneRef.FromIndex(buildIndex));
        }
    }
}