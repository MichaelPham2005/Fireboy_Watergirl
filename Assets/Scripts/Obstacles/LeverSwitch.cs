using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class LeverSwitch : MonoBehaviour
{
    public GateController linkedGate;
    
    [Header("Rotation Settings")]
    [Tooltip("Góc xoay (Rotation Z) khi cần gạt ngả sang Tận Cùng Bên Trái")]
    public float leftAngle = 90f;
    [Tooltip("Góc xoay (Rotation Z) khi cần gạt ngả sang Tận Cùng Bên Phải")]
    public float rightAngle = 0f;
    
    [Tooltip("Gạt sang Phải là MỞ cổng? (Bỏ tick nếu gạt sang Trái là Mở cổng)")]
    public bool rightIsOn = true;

    [Tooltip("Tốc độ gạt (Càng cao đẩy càng nhẹ)")]
    public float pushSpeed = 400f;

    private float currentAngle;
    private bool isTurnedOn = false;
    private float lastPushedTime = 0f;

    void Start()
    {
        // Chuyển Rigidbody thành Kinematic để nó không bị rớt, nhưng vẫn cản đường người chơi!
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.useFullKinematicContacts = true; 
        }

        // Đọc góc hiện tại trong Scene để KHÔNG reset cần gạt! 
        // Cho phép bạn đặt nghiêng tự do trong Scene.
        float startZ = transform.localEulerAngles.z;
        
        // Tự động neo vào góc Trái hoặc Phải tùy xem lúc đầu bạn đặt nó nghiêng về bên nào hơn
        float distToLeft = Mathf.Abs(Mathf.DeltaAngle(startZ, leftAngle));
        float distToRight = Mathf.Abs(Mathf.DeltaAngle(startZ, rightAngle));

        currentAngle = (distToLeft < distToRight) ? leftAngle : rightAngle;
        transform.localRotation = Quaternion.Euler(0, 0, currentAngle);

        // Khởi tạo trạng thái ban đầu của cổng
        bool isCurrentlyRight = Mathf.Approximately(currentAngle, rightAngle);
        isTurnedOn = (isCurrentlyRight == rightIsOn);
    }

    void Update()
    {
        // Nếu người chơi thả ra không đẩy nữa trong khoảng 0.1s, tự động hút (snap) về trạng thái gần nhất
        if (Time.time - lastPushedTime > 0.1f)
        {
            float distToLeft = Mathf.Abs(Mathf.DeltaAngle(currentAngle, leftAngle));
            float distToRight = Mathf.Abs(Mathf.DeltaAngle(currentAngle, rightAngle));
            float targetAngle = (distToLeft < distToRight) ? leftAngle : rightAngle;
            
            if (!Mathf.Approximately(currentAngle, targetAngle))
            {
                currentAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, pushSpeed * 0.8f * Time.deltaTime);
                transform.localRotation = Quaternion.Euler(0, 0, currentAngle);
                CheckGate();
            }
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        ProcessPush(collision.gameObject);

        // --- HACK ĐỂ CHỐNG BAY LÊN TRỜI ---
        // Khi nhân vật húc vào một vật thể chéo (như cần gạt), Unity Physics sẽ tự động đẩy nhân vật trượt lên dốc.
        // Đoạn code này sẽ dập tắt lực đẩy đó, ghim nhân vật xuống đất trừ khi họ chủ động bấm nút Nhảy.
        Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            // Nếu vận tốc Y đang đi lên nhưng nhỏ hơn 5 (tức là bị trượt lên chứ không phải do Nhảy)
            if (playerRb.linearVelocity.y > 0f && playerRb.linearVelocity.y < 5f)
            {
                // Ép vận tốc Y về 0 (hoặc hơi âm một chút) để ghim nhân vật lại không cho bay lên
                playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, -0.5f);
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        ProcessPush(collision.gameObject);
    }

    private void ProcessPush(GameObject player)
    {
        if (player.CompareTag("Fireboy") || player.CompareTag("Watergirl"))
        {
            lastPushedTime = Time.time;
            
            // Người chơi đứng bên trái, đẩy sang Phải
            if (player.transform.position.x < transform.position.x)
            {
                currentAngle = Mathf.MoveTowardsAngle(currentAngle, rightAngle, pushSpeed * Time.deltaTime);
            }
            // Người chơi đứng bên phải, đẩy sang Trái
            else
            {
                currentAngle = Mathf.MoveTowardsAngle(currentAngle, leftAngle, pushSpeed * Time.deltaTime);
            }

            transform.localRotation = Quaternion.Euler(0, 0, currentAngle);
            CheckGate();
        }
    }

    private void CheckGate()
    {
        bool isCurrentlyRight = Mathf.Approximately(currentAngle, rightAngle);
        bool isCurrentlyLeft = Mathf.Approximately(currentAngle, leftAngle);

        if (isCurrentlyRight)
        {
            bool shouldBeOn = rightIsOn;
            if (isTurnedOn != shouldBeOn)
            {
                isTurnedOn = shouldBeOn;
                if (isTurnedOn) linkedGate?.OpenGate();
                else linkedGate?.CloseGate();
            }
        }
        else if (isCurrentlyLeft)
        {
            bool shouldBeOn = !rightIsOn;
            if (isTurnedOn != shouldBeOn)
            {
                isTurnedOn = shouldBeOn;
                if (isTurnedOn) linkedGate?.OpenGate();
                else linkedGate?.CloseGate();
            }
        }
    }
}
