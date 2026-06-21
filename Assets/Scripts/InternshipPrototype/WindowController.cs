using UnityEngine;

/// <summary>
/// Opens and closes one UI window.
/// Attach this component to an object that always stays active, such as Desktop.
/// </summary>
public class WindowController : MonoBehaviour
{
    [SerializeField] private GameObject windowPanel;

    private void Start()
    {
        // The window starts hidden when the game begins.
        CloseWindow();
    }

    public void OpenWindow()
    {
        if (windowPanel != null)
            windowPanel.SetActive(true);
    }

    public void CloseWindow()
    {
        if (windowPanel != null)
            windowPanel.SetActive(false);
    }

    public void ToggleWindow()
    {
        if (windowPanel != null)
            windowPanel.SetActive(!windowPanel.activeSelf);
    }
}
