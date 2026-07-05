using UnityEngine;

public class PulleyManager : MonoBehaviour
{
    public Rigidbody2D platformLeft;
    public Rigidbody2D platformRight;

    private float stringLengthConst;

    void Start()
    {
        // Tính tổng tọa độ Y ban đầu. Trong hệ ròng rọc hoàn hảo, tổng Y của 2 đầu dây luôn không đổi.
        stringLengthConst = platformLeft.position.y + platformRight.position.y;
    }

    void FixedUpdate()
    {
        // 1. Chia đều vận tốc để ép chúng chạy ngược chiều nhau
        // Nếu bên phải bị đá đè (velocity.y âm mạnh hơn), coupledVel sẽ tạo lực kéo bên trái lên
        float coupledVel = (platformLeft.linearVelocity.y - platformRight.linearVelocity.y) / 2f;

        // 2. Chống dãn dây: Tính toán sai số vị trí để tự động kéo chúng về chuẩn
        float error = stringLengthConst - (platformLeft.position.y + platformRight.position.y);
        float correction = error * 5f; 

        // 3. Áp dụng vận tốc mới cho cả 2 sàn
        platformLeft.linearVelocity = new Vector2(platformLeft.linearVelocity.x, coupledVel + correction);
        platformRight.linearVelocity = new Vector2(platformRight.linearVelocity.x, -coupledVel + correction);
    }
}