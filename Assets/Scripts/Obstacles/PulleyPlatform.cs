using UnityEngine;
using System.Collections.Generic;

public class PulleyPlatform : MonoBehaviour
{
    [HideInInspector] public float totalMassOnPlatform = 0f;
    private Collider2D col;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
    }

    private void FixedUpdate()
    {
        if (col == null) return;

        // Dùng OverlapBox thay vì OnCollision để tránh lỗi Feedback Loop
        // (Khi ròng rọc đi xuống, nhân vật bị hở ra 1 li làm mất va chạm -> ròng rọc dừng -> nhân vật rớt xuống đụng lại -> ròng rọc chạy tiếp -> Jitter)
        Vector2 size = col.bounds.size;
        size.x *= 0.95f; 
        size.y += 0.5f; // Mở rộng vùng check lên trên 0.5 units để bắt các vật thể đang bị "hở" nhẹ do ròng rọc đi xuống
        
        Vector2 center = col.bounds.center;
        center.y += 0.25f;

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f);

        float currentMass = 0f;
        HashSet<Rigidbody2D> processedRbs = new HashSet<Rigidbody2D>();

        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject == gameObject) continue;

            Rigidbody2D rb = hit.attachedRigidbody;
            if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
            {
                if (!processedRbs.Contains(rb))
                {
                    // Chỉ tính những vật có tâm nằm cao hơn mép dưới của platform
                    if (hit.bounds.center.y > col.bounds.min.y)
                    {
                        currentMass += rb.mass;
                        processedRbs.Add(rb);
                    }
                }
            }
        }

        totalMassOnPlatform = currentMass;
    }
}
