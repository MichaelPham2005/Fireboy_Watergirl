using UnityEngine;
using UnityEngine.UI;

public class EquipButton : MonoBehaviour
{
    private DressUpManager manager;

    [Header("Định danh món đồ này")]
    public int slotIndex;    // Thuộc nhóm nào? (Ví dụ: 0 là Cà vạt, 1 là Nón)
    public int versionIndex; // Màu sắc/Phiên bản số mấy? (0 là Đỏ, 1 là Xanh, 2 là Xanh lá)

    void Start()
    {
        // Tự động tìm DressUpManager ở Object cha
        manager = GetComponentInParent<DressUpManager>();
        
        // Tự động gán sự kiện Click chuột mà không cần kéo thả tay
        GetComponent<Button>().onClick.AddListener(OnButtonClick);
    }

    void OnButtonClick()
    {
        if (manager != null)
        {
            manager.EquipItem(slotIndex, versionIndex);
        }
    }
}