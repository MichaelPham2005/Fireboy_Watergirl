using UnityEngine;

public class ShadowGuardianAI : MonoBehaviour
{
    [Header("Mục tiêu")]
    public Transform fireboy;
    public Transform watergirl;
    private Transform currentTarget;

    [Header("Thông số Di chuyển")]
    public float speed = 2.5f;
    public float switchInterval = 10f; // Thời gian đếm ngược để đổi mục tiêu
    private float timer = 0f;

    [Header("Hiển thị & Màu sắc")]
    public SpriteRenderer spriteRenderer;
    public Color fireColor = Color.red;
    public Color waterColor = Color.blue;

    private bool huntingFireboy = true;

    void Start()
    {
        // Thiết lập trạng thái đầu tiên khi vào màn chơi
        currentTarget = fireboy;
        spriteRenderer.color = fireColor;
    }

    void Update()
    {
        // 1. Logic đếm giờ (Timer) để chuyển đổi trạng thái
        timer += Time.deltaTime;
        if (timer >= switchInterval)
        {
            SwitchTarget();
            timer = 0f; // Reset lại bộ đếm
        }

        // 2. Logic di chuyển tịnh tiến về phía mục tiêu
        if (currentTarget != null)
        {
            transform.position = Vector2.MoveTowards(
                transform.position, 
                currentTarget.position, 
                speed * Time.deltaTime
            );
        }
    }

    // Hàm xử lý logic đảo mục tiêu
    void SwitchTarget()
    {
        huntingFireboy = !huntingFireboy;
        
        // Đảo cờ và gán lại Transform tương ứng
        currentTarget = huntingFireboy ? fireboy : watergirl;
        
        // Đổi màu để cảnh báo người chơi
        spriteRenderer.color = huntingFireboy ? fireColor : waterColor;
    }

    // Nhận diện khi chạm vào người chơi
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Kiểm tra nếu đối tượng chạm vào mang Tag "Fireboy" HOẶC "Watergirl"
        if (collision.CompareTag("Fireboy") || collision.CompareTag("Watergirl"))
        {
            Debug.Log("Game Over! Quái đã chạm vào " + collision.name);
            PlayerHealth health = collision.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.Die();
            }
        }
    }
}