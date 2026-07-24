using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Attach to each menu Button GameObject.
/// - Scales up the text + brightens + bolds on mouse hover.
/// - Optionally changes cursor to a pointer image.
///
/// SETUP:
///   1. Drag this script onto each of the 6 menu buttons.
///   2. (Optional) Assign a pointer cursor texture to "Cursor Hand Texture".
///   3. MenuNavigator will call SetHighlighted() automatically for keyboard nav.
/// </summary>
public class MenuButtonHover : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover Settings")]
    [Tooltip("How much to scale up the text on hover (1.1 = 10% bigger).")]
    public float hoverScale = 1.1f;

    [Tooltip("Optional: Drag in a pointer/hand cursor image (PNG with transparent bg).")]
    public Texture2D cursorHandTexture;

    // -------------------------------------------------------------------------
    private TextMeshProUGUI    label;
    private Vector3            originalScale;
    private FontStyles         originalFontStyle;
    private Color              originalColor;

    // -------------------------------------------------------------------------

    private void Awake()
    {
        label = GetComponentInChildren<TextMeshProUGUI>();

        if (label != null)
        {
            originalScale     = label.transform.localScale;
            originalFontStyle = label.fontStyle;
            originalColor     = label.color;

            // CRITICAL FIX: Disable word wrapping so Bold text doesn't force a new line
            label.enableWordWrapping = false;
            label.overflowMode       = TextOverflowModes.Overflow;
        }
    }

    // --- Mouse hover ---

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Tell the navigator which button the mouse is over
        MenuNavigator nav = FindAnyObjectByType<MenuNavigator>();
        if (nav != null) nav.SetSelectedByButton(this);

        // Change cursor to hand if texture provided
        if (cursorHandTexture != null)
            Cursor.SetCursor(cursorHandTexture, new Vector2(8, 2), CursorMode.Auto);

        SetHighlighted(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (cursorHandTexture != null)
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        // Reset highlight when mouse leaves (unless keyboard has taken over)
        MenuNavigator nav = FindAnyObjectByType<MenuNavigator>();
        if (nav == null || !nav.IsKeyboardControlling)
            SetHighlighted(false);
    }

    // --- Called by MenuNavigator for keyboard nav ---

    public void SetHighlighted(bool on)
    {
        if (label == null) return;

        if (on)
        {
            label.transform.localScale = originalScale * hoverScale;
            label.fontStyle = originalFontStyle | FontStyles.Bold;
            label.color     = new Color(
                Mathf.Min(originalColor.r * 1.25f, 1f),
                Mathf.Min(originalColor.g * 1.25f, 1f),
                Mathf.Min(originalColor.b * 1.25f, 1f),
                originalColor.a
            );
        }
        else
        {
            label.transform.localScale = originalScale;
            label.fontStyle = originalFontStyle;
            label.color     = originalColor;
        }
    }
}
