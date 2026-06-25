using UnityEngine;

/// <summary>
/// Opens and closes one UI window.
/// Attach this component to an object that always stays active, such as Desktop.
/// </summary>
public class WindowController : MonoBehaviour
{
    private const float DoubleClickWindow = 0.35f;
    private const string DesktopButtonsName = "DesktopButtons";

    [SerializeField] private GameObject windowPanel;

    private float lastOpenClickTime = -1f;
    private GameObject lastOpenClickTarget;
    private GameObject currentSelectedWindow;

    private void Start()
    {
        HideDesktopWindows();
    }

    public void OpenWindow()
    {
        OpenSelectedWindow(windowPanel);
    }

    public void OpenWindowOnDoubleClick()
    {
        if (RegisterDoubleClick(windowPanel))
            OpenWindow();
    }

    public void OpenSelectedWindow(GameObject targetWindow)
    {
        if (targetWindow == null)
            return;

        targetWindow.SetActive(true);
        targetWindow.transform.SetAsLastSibling();

        currentSelectedWindow = FindDesktopWindow(targetWindow);
        currentSelectedWindow.SetActive(true);
        currentSelectedWindow.transform.SetAsLastSibling();
    }

    public void OpenSelectedWindowOnDoubleClick(GameObject targetWindow)
    {
        if (RegisterDoubleClick(targetWindow))
            OpenSelectedWindow(targetWindow);
    }

    public void CloseWindow()
    {
        CloseSelectedWindow(windowPanel);
    }

    public void CloseSelectedWindow(GameObject targetWindow)
    {
        if (targetWindow == null)
            return;

        targetWindow.SetActive(false);

        if (currentSelectedWindow == targetWindow || currentSelectedWindow == FindDesktopWindow(targetWindow))
            currentSelectedWindow = null;
    }

    public void ToggleWindow()
    {
        if (windowPanel != null)
            windowPanel.SetActive(!windowPanel.activeSelf);
    }

    private void HideDesktopWindows()
    {
        foreach (Transform child in transform)
        {
            if (child.name == DesktopButtonsName)
                continue;

            child.gameObject.SetActive(false);
        }
    }

    private GameObject FindDesktopWindow(GameObject targetWindow)
    {
        Transform targetTransform = targetWindow.transform;
        Transform desktopTransform = transform;

        while (targetTransform.parent != null && targetTransform.parent != desktopTransform)
        {
            targetTransform = targetTransform.parent;
        }

        return targetTransform.gameObject;
    }

    private bool RegisterDoubleClick(GameObject targetWindow)
    {
        float clickTime = Time.unscaledTime;
        bool isSameTarget = targetWindow == lastOpenClickTarget;
        bool isDoubleClick = isSameTarget && clickTime - lastOpenClickTime <= DoubleClickWindow;

        lastOpenClickTime = clickTime;
        lastOpenClickTarget = targetWindow;

        if (!isDoubleClick)
            return false;

        lastOpenClickTime = -1f;
        lastOpenClickTarget = null;
        return true;
    }
}
