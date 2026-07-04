using UnityEngine;
using System.Collections; // Cần dòng này để dùng Coroutine

public class PlayerHealth : MonoBehaviour
{
    [Header("Components")]
    public StandardPlayerMovement movementScript;
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        if (movementScript == null) movementScript = GetComponent<StandardPlayerMovement>();
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        PoolElement pool = col.GetComponent<PoolElement>();
        if (pool != null) CheckDeath(pool);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        PoolElement pool = col.gameObject.GetComponent<PoolElement>();
        if (pool != null && ShouldDieFromCollision(col)) CheckDeath(pool);
    }

    void OnCollisionStay2D(Collision2D col)
    {
        PoolElement pool = col.gameObject.GetComponent<PoolElement>();
        if (pool != null && ShouldDieFromCollision(col)) CheckDeath(pool);
    }

    private bool ShouldDieFromCollision(Collision2D col)
    {
        // FIX: Nếu đang đứng trên cần gạt thì KHÔNG chết!
        // (Dù cần gạt có là vật liệu "lỏng" đi nữa, ta vẫn coi là an toàn)
        if (col.gameObject.GetComponent<LeverSwitch>() != null || 
            col.gameObject.GetComponentInParent<LeverSwitch>() != null)
        {
            return false;
        }
        
        // Chỉ chết nếu va chạm từ phía trên (mặt chất lỏng)
        foreach (ContactPoint2D contact in col.contacts)
        {
            if (contact.normal.y > 0.1f) return true;
        }
        return false;
    }



    private void CheckDeath(PoolElement pool)
    {
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

        // Only start death routine if we should die and haven't already died (movementScript is still enabled)
        if (shouldDie && movementScript != null && movementScript.enabled)
        {
            StartCoroutine(DieWithDelay(0.1f)); 
        }
    }

    IEnumerator DieWithDelay(float delay)
    {
        // 1. Disable movement immediately
        if (movementScript != null) movementScript.enabled = false;
        
        // 2. Trigger the Death animation on the body, and simply HIDE the head
        if (movementScript != null)
        {
            if (movementScript.bodyAnimator != null) 
                movementScript.bodyAnimator.SetTrigger("Die");
                
            if (movementScript.headAnimator != null) 
                movementScript.headAnimator.gameObject.SetActive(false); // Make head disappear instantly
        }

        // 3. Wait for the slide delay (if you want them to slide down slopes while smoking)
        yield return new WaitForSeconds(delay);

        // 4. Stop physics simulation completely
        Debug.Log(gameObject.name + " evaporated!");
        if (rb != null) rb.simulated = false;
        
        // NOTE: We no longer disable the SpriteRenderer here (sr.enabled = false).
        // The Death animation clip will handle fading out or hiding the sprite.
    }
}