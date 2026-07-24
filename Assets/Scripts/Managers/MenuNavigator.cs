using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Keyboard navigation for the main menu.
/// Attach to the same GameObject as MainMenuManager.
///
/// SETUP:
///   1. Attach this script to the MenuHandler (or any always-active GameObject).
///   2. In the Inspector, drag the 6 menu buttons into "Menu Buttons" (in order top→bottom):
///        Level1 → Level2 → Level3 → Level4 → Ranking → Play Online
///   3. Make sure each button also has MenuButtonHover attached.
/// </summary>
public class MenuNavigator : MonoBehaviour
{
    [Header("Menu Buttons (top → bottom order)")]
    [Tooltip("Drag all menu buttons here in the order they appear on screen.")]
    public Button[] menuButtons;

    [Header("Keys")]
    public KeyCode upKey    = KeyCode.UpArrow;
    public KeyCode downKey  = KeyCode.DownArrow;
    public KeyCode confirmKey = KeyCode.Return;
    public KeyCode confirmKeyAlt = KeyCode.KeypadEnter;

    // -------------------------------------------------------------------------
    private int currentIndex = -1; // -1 = no selection yet (mouse-only mode)
    private MenuButtonHover[] hovers;

    /// <summary>True after the user pressed an arrow key; false once mouse takes over.</summary>
    public bool IsKeyboardControlling { get; private set; }

    // -------------------------------------------------------------------------

    private void Start()
    {
        // Cache the hover components that sit on each button
        hovers = new MenuButtonHover[menuButtons.Length];
        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] != null)
                hovers[i] = menuButtons[i].GetComponent<MenuButtonHover>();
        }

        // Highlight first item by default (keyboard ready)
        // We don't highlight at Start so mouse users don't see a random item lit.
        // Highlighting only kicks in once the user presses a key.
    }

    private void Update()
    {
        if (menuButtons == null || menuButtons.Length == 0) return;

        bool movedUp   = Input.GetKeyDown(upKey);
        bool movedDown = Input.GetKeyDown(downKey);
        bool confirmed = Input.GetKeyDown(confirmKey) || Input.GetKeyDown(confirmKeyAlt);

        if (movedUp || movedDown)
        {
            IsKeyboardControlling = true;

            // On first key press, start from index 0
            if (currentIndex < 0) currentIndex = 0;
            else
            {
                // Clear current highlight
                SetHighlight(currentIndex, false);

                if (movedUp)
                    currentIndex = (currentIndex - 1 + menuButtons.Length) % menuButtons.Length;
                else
                    currentIndex = (currentIndex + 1) % menuButtons.Length;
            }

            // Apply new highlight
            SetHighlight(currentIndex, true);
        }

        if (confirmed && currentIndex >= 0)
        {
            Button btn = menuButtons[currentIndex];
            if (btn != null && btn.interactable)
                btn.onClick.Invoke();
        }
    }

    /// <summary>Called by MenuButtonHover when the mouse enters a button.</summary>
    public void SetSelectedByButton(MenuButtonHover hoveredButton)
    {
        IsKeyboardControlling = false;

        // Clear old keyboard highlight (if any)
        SetHighlight(currentIndex, false);

        // Find the new index
        for (int i = 0; i < hovers.Length; i++)
        {
            if (hovers[i] == hoveredButton)
            {
                currentIndex = i;
                break;
            }
        }
        // (The hover script already calls SetHighlighted itself on pointer enter)
    }

    private void SetHighlight(int index, bool on)
    {
        if (index < 0 || index >= hovers.Length) return;
        if (hovers[index] != null)
            hovers[index].SetHighlighted(on);
    }
}
