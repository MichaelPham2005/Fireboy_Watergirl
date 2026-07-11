using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Khai báo cấu trúc nhóm để hiển thị đẹp trên Inspector
[System.Serializable]
public class AccessorySlot
{
    public string slotName;       // Tên nhóm (Ví dụ: Cà Vạt, Nón)
    public string prefsKey;       // Từ khóa để lưu máy (Ví dụ: FB_Tie, FB_Hat)
    public GameObject[] versions; // Mảng chứa các GameObject Đỏ, Xanh, Vàng
}

public class DressUpManager : MonoBehaviour
{
    [Header("Các Vị Trí Trang Bị Của Nhân Vật")]
    public AccessorySlot[] customizationSlots;

    void Awake()
    {
        InitializeSlots();
    }

    void OnEnable()
    {
        // Đảm bảo slots được khởi tạo
        InitializeSlots();

        // Tự động sinh ra các Button trong UI
        GenerateUIButtons();

        // Đọc dữ liệu cũ để hiển thị đúng đồ đang mặc
        if (customizationSlots != null)
        {
            for (int i = 0; i < customizationSlots.Length; i++)
            {
                if (customizationSlots[i] == null) continue;
                int savedVersion = PlayerPrefs.GetInt(customizationSlots[i].prefsKey, -1);
                ApplyItemToPreview(i, savedVersion);
            }
        }
    }

    [Header("Cấu hình Slot mục tiêu")]
    public string targetSlotName = "Slot_Neck";

    private void InitializeSlots()
    {
        if (customizationSlots == null || customizationSlots.Length == 0)
        {
            string actualSlotName = string.IsNullOrEmpty(targetSlotName) ? "Slot_Neck" : targetSlotName;
            GameObject slotNeckGo = GameObject.Find(actualSlotName);
            if (slotNeckGo != null)
            {
                bool isWatergirl = actualSlotName.ToLower().Contains("watergirl");
                customizationSlots = new AccessorySlot[1];
                customizationSlots[0] = new AccessorySlot
                {
                    slotName = isWatergirl ? "Bowtie" : "Tie",
                    prefsKey = isWatergirl ? "WG_Tie" : "FB_Tie"
                };

                string prefix = isWatergirl ? "bowtie_" : "tie_";
                var versionsList = new System.Collections.Generic.List<GameObject>();
                for (int i = 0; i < slotNeckGo.transform.childCount; i++)
                {
                    GameObject childGo = slotNeckGo.transform.GetChild(i).gameObject;
                    if (childGo.name.ToLower().StartsWith(prefix))
                    {
                        versionsList.Add(childGo);
                    }
                }

                customizationSlots[0].versions = versionsList.ToArray();

                // Đảm bảo mỗi phiên bản có SpriteRenderer để hiển thị đúng sprite và màu sắc
                Sprite tieSprite = Resources.Load<Sprite>(isWatergirl ? "bow-tie" : "tie");
                SpriteRenderer parentSr = slotNeckGo.GetComponentInParent<SpriteRenderer>();
                if (parentSr == null && slotNeckGo.transform.parent != null)
                {
                    parentSr = slotNeckGo.transform.parent.GetComponentInChildren<SpriteRenderer>();
                }

                for (int j = 0; j < versionsList.Count; j++)
                {
                    GameObject versionGo = versionsList[j];
                    if (versionGo == null) continue;

                    SpriteRenderer sr = versionGo.GetComponent<SpriteRenderer>();
                    if (sr == null)
                    {
                        sr = versionGo.AddComponent<SpriteRenderer>();
                    }

                    if (tieSprite != null)
                    {
                        sr.sprite = tieSprite;
                    }

                    if (parentSr != null)
                    {
                        sr.sortingLayerID = parentSr.sortingLayerID;
                        sr.sortingOrder = parentSr.sortingOrder + 1;
                    }
                    else
                    {
                        sr.sortingOrder = 10;
                    }

                    // Đặt màu dựa trên tên hoặc index của phiên bản
                    Color tieColor = Color.white;
                    string nameLower = versionGo.name.ToLower();
                    if (nameLower.Contains("white")) tieColor = Color.white;
                    else if (nameLower.Contains("blue")) tieColor = new Color(0f, 0f, 0.867f, 1f);
                    else if (nameLower.Contains("pink")) tieColor = new Color(1f, 0f, 0.708f, 1f);
                    else if (nameLower.Contains("green")) tieColor = new Color(0f, 0.83f, 0.199f, 1f);
                    else
                    {
                        switch (j)
                        {
                            case 0: tieColor = Color.white; break;
                            case 1: tieColor = new Color(0f, 0f, 0.867f, 1f); break; // Blue
                            case 2: tieColor = new Color(1f, 0f, 0.708f, 1f); break; // Pink
                            case 3: tieColor = new Color(0f, 0.83f, 0.199f, 1f); break; // Green
                        }
                    }
                    sr.color = tieColor;
                }

                Debug.Log($"Successfully auto-initialized {versionsList.Count} neck slots from {actualSlotName} hierarchy and configured SpriteRenderers!");
            }
            else
            {
                Debug.LogWarning($"{actualSlotName} GameObject not found in the scene!");
            }
        }
    }

    private void GenerateUIButtons()
    {
        // Kiểm tra xem các nút đã có sẵn trong Hierarchy chưa (trường hợp tạo sẵn trong Edit Mode)
        bool alreadyPresent = false;
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Btn_Tie_") || child.name.StartsWith("Btn_Bowtie_"))
            {
                alreadyPresent = true;
                break;
            }
        }

        if (alreadyPresent)
        {
            Transform tempTransform = transform.Find("Button");
            if (tempTransform != null)
            {
                tempTransform.gameObject.SetActive(false);
            }
            return;
        }

        // DỌN DẸP BẢO VỆ: Xóa hết các button cũ được sinh ra trước đó để tránh lặp button
        foreach (Transform child in transform)
        {
            if (child.name != "Button")
            {
                Destroy(child.gameObject);
            }
        }

        // Tìm template button làm mẫu
        Transform templateTransform = transform.Find("Button");
        if (templateTransform == null)
        {
            Button childButton = GetComponentInChildren<Button>(true);
            if (childButton != null)
            {
                templateTransform = childButton.transform;
            }
        }

        if (templateTransform == null)
            return;

        if (customizationSlots == null || customizationSlots.Length == 0) return;

        AccessorySlot tieSlot = customizationSlots[0];
        if (tieSlot == null || tieSlot.versions == null) return;

        bool isWatergirl = targetSlotName.ToLower().Contains("watergirl");
        Sprite tieSprite = Resources.Load<Sprite>(isWatergirl ? "bow-tie" : "tie");

        for (int j = 0; j < tieSlot.versions.Length; j++)
        {
            GameObject versionGo = tieSlot.versions[j];
            if (versionGo == null) continue;

            // Nhân bản template
            GameObject newButtonGo = Instantiate(templateTransform.gameObject, transform);
            newButtonGo.name = (isWatergirl ? "Btn_Bowtie_" : "Btn_Tie_") + versionGo.name;
            newButtonGo.SetActive(true);

            // Gán hoặc thêm component EquipButton
            EquipButton equipBtn = newButtonGo.GetComponent<EquipButton>();
            if (equipBtn == null)
            {
                equipBtn = newButtonGo.AddComponent<EquipButton>();
            }
            equipBtn.slotIndex = 0;
            equipBtn.versionIndex = j;

            // Thiết lập nền của button là một ô vuông/chữ nhật màu tối/sáng nhẹ nhàng
            Image btnImage = newButtonGo.GetComponent<Image>();
            if (btnImage != null)
            {
                btnImage.sprite = null; // Loại bỏ hình dạng tie bị kéo giãn
                btnImage.color = new Color(0.15f, 0.15f, 0.15f, 0.6f); // Nền tối trong suốt cao cấp
            }

            // Tạo icon Cà vạt / Nơ hiển thị ở chính giữa nút
            if (tieSprite != null)
            {
                GameObject iconGo = new GameObject("TieIcon");
                iconGo.transform.SetParent(newButtonGo.transform, false);

                Image iconImage = iconGo.AddComponent<Image>();
                iconImage.sprite = tieSprite;
                iconImage.preserveAspect = true;

                // Đồng bộ màu của icon với màu SpriteRenderer của tie version đó
                SpriteRenderer sr = versionGo.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    iconImage.color = sr.color;
                }

                // Căn chỉnh tỉ lệ icon cà vạt / nơ nằm gọn gàng bên trong button
                RectTransform iconRect = iconGo.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.pivot = new Vector2(0.5f, 0.5f);
                // Nơ (bowtie) rộng ngang, Cà vạt (tie) dài dọc
                iconRect.sizeDelta = isWatergirl ? new Vector2(30f, 25f) : new Vector2(25f, 30f);
            }

            // Ẩn Text của nút vì màu sắc cà vạt đã tự giải thích trực quan (giống như reference)
            TextMeshProUGUI txt = newButtonGo.GetComponentInChildren<TextMeshProUGUI>(true);
            if (txt != null)
            {
                txt.gameObject.SetActive(false);
            }
        }

        // Ẩn button template gốc
        templateTransform.gameObject.SetActive(false);
        Debug.Log("Successfully generated custom item buttons dynamically!");
    }

    public void EquipItem(int slotIndex, int versionIndex)
    {
        if (customizationSlots == null || slotIndex < 0 || slotIndex >= customizationSlots.Length) return;
        AccessorySlot currentSlot = customizationSlots[slotIndex];
        if (currentSlot == null) return;

        // Nếu bấm lại vào món đang mặc -> Cởi ra (gán về -1)
        if (PlayerPrefs.GetInt(currentSlot.prefsKey, -1) == versionIndex)
        {
            versionIndex = -1;
        }

        // Cập nhật lên nhân vật hiển thị và lưu lại
        ApplyItemToPreview(slotIndex, versionIndex);
        PlayerPrefs.SetInt(currentSlot.prefsKey, versionIndex);
        PlayerPrefs.Save();
    }

    private void ApplyItemToPreview(int slotIndex, int versionIndex)
    {
        if (customizationSlots == null || slotIndex < 0 || slotIndex >= customizationSlots.Length) return;
        AccessorySlot currentSlot = customizationSlots[slotIndex];
        if (currentSlot == null || currentSlot.versions == null) return;

        // Tắt toàn bộ các phiên bản trong slot này trước
        foreach (GameObject item in currentSlot.versions)
        {
            if (item != null) item.SetActive(false);
        }

        // Bật đúng phiên bản được chọn lên
        if (versionIndex >= 0 && versionIndex < currentSlot.versions.Length)
        {
            if (currentSlot.versions[versionIndex] != null)
            {
                currentSlot.versions[versionIndex].SetActive(true);
            }
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Setup Buttons In Scene")]
    public void SetupButtonsInScene()
    {
        // Khởi tạo các slot và liên kết
        InitializeSlots();

        if (customizationSlots == null || customizationSlots.Length == 0)
        {
            Debug.LogError("No customization slots found! Ensure Slot_Neck is in the scene.");
            return;
        }

        // Tìm template button
        Transform templateTransform = transform.Find("Button");
        if (templateTransform == null)
        {
            Button childButton = GetComponentInChildren<Button>(true);
            if (childButton != null)
            {
                templateTransform = childButton.transform;
            }
        }

        if (templateTransform == null)
        {
            Debug.LogError("No template Button found!");
            return;
        }

        // Dọn các button cũ
        System.Collections.Generic.List<GameObject> oldButtons = new System.Collections.Generic.List<GameObject>();
        foreach (Transform child in transform)
        {
            if (child.name != "Button")
            {
                oldButtons.Add(child.gameObject);
            }
        }
        foreach (GameObject oldBtn in oldButtons)
        {
            DestroyImmediate(oldBtn);
        }

        AccessorySlot tieSlot = customizationSlots[0];
        bool isWatergirl = targetSlotName.ToLower().Contains("watergirl");
        Sprite tieSprite = AssetDatabase.LoadAssetAtPath<Sprite>(isWatergirl ? "Assets/Resources/bow-tie.png" : "Assets/Resources/tie.png");

        for (int j = 0; j < tieSlot.versions.Length; j++)
        {
            GameObject versionGo = tieSlot.versions[j];
            if (versionGo == null) continue;

            // Nhân bản
            GameObject newButtonGo = (GameObject)PrefabUtility.InstantiatePrefab(templateTransform.gameObject);
            if (newButtonGo == null)
            {
                newButtonGo = Instantiate(templateTransform.gameObject, transform);
            }
            else
            {
                newButtonGo.transform.SetParent(transform, false);
            }

            newButtonGo.name = (isWatergirl ? "Btn_Bowtie_" : "Btn_Tie_") + versionGo.name;
            newButtonGo.SetActive(true);

            // Ghi nhận Undo
            Undo.RegisterCreatedObjectUndo(newButtonGo, "Create Customization Button");

            // Setup EquipButton
            EquipButton equipBtn = newButtonGo.GetComponent<EquipButton>();
            if (equipBtn == null)
            {
                equipBtn = newButtonGo.AddComponent<EquipButton>();
            }
            equipBtn.slotIndex = 0;
            equipBtn.versionIndex = j;

            // Nền button
            Image btnImage = newButtonGo.GetComponent<Image>();
            if (btnImage != null)
            {
                btnImage.sprite = null;
                btnImage.color = new Color(0.15f, 0.15f, 0.15f, 0.6f);
            }

            // Tạo icon tie
            if (tieSprite != null)
            {
                GameObject iconGo = new GameObject("TieIcon");
                iconGo.transform.SetParent(newButtonGo.transform, false);

                Image iconImage = iconGo.AddComponent<Image>();
                iconImage.sprite = tieSprite;
                iconImage.preserveAspect = true;

                SpriteRenderer sr = versionGo.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    iconImage.color = sr.color;
                }

                RectTransform iconRect = iconGo.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.pivot = new Vector2(0.5f, 0.5f);
                iconRect.sizeDelta = isWatergirl ? new Vector2(30f, 25f) : new Vector2(25f, 30f);
            }

            // Ẩn Text
            TextMeshProUGUI txt = newButtonGo.GetComponentInChildren<TextMeshProUGUI>(true);
            if (txt != null)
            {
                txt.gameObject.SetActive(false);
            }
        }

        // Ẩn template
        templateTransform.gameObject.SetActive(false);

        // Đánh dấu bẩn để lưu
        EditorUtility.SetDirty(gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);

        Debug.Log("Buttons successfully generated in Edit Mode! Save the scene to keep changes.");
    }
#endif
}