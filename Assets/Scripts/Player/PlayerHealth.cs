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
        if (pool != null)
        {
            if (pool.liquidType == LiquidType.BlueWater || pool.liquidType == LiquidType.GreenGoo)
            {
                // Gọi một Coroutine để tạo độ trễ
                StartCoroutine(DieWithDelay(0.5f)); // Trễ 0.5 giây để nhân vật kịp trượt xuống dốc
            }
        }
    }

    IEnumerator DieWithDelay(float delay)
    {
        // 1. Tắt di chuyển ngay
        if (movementScript != null) movementScript.enabled = false;
        
        // 2. Cho phép nhân vật trượt xuống dốc thêm 0.5 giây nữa
        yield return new WaitForSeconds(delay);

        // 3. Thực hiện bốc hơi
        Debug.Log("Fireboy evaporated!");
        if (rb != null) rb.simulated = false;
        if (sr != null) sr.enabled = false;
    }
}