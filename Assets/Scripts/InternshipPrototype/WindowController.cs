using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Opens and closes one UI window.
/// Attach this component to an object that always stays active, such as Desktop.
/// </summary>
public class WindowController : MonoBehaviour
{
    private const float DoubleClickWindow = 0.35f;
    private const string DesktopButtonsName = "DesktopButtons";
    private const string DailyTasksName = "DailyTasks";

    [SerializeField] private GameObject windowPanel;
    [SerializeField] private GameObject[] alwaysVisibleObjects;

    private float lastOpenClickTime = -1f;
    private GameObject lastOpenClickTarget;
    private GameObject currentSelectedWindow;

    private void Start()
    {
        HideDesktopWindows();
        InstallDesktopWindowFocusTargets();
    }

    public void OpenWindow()
    {
        OpenSelectedWindow(windowPanel);
    }

    public void OpenWindowOnDoubleClick()
    {
        OpenSelectedWindowOnDoubleClick(windowPanel);
    }

    public void OpenSelectedWindow(GameObject targetWindow)
    {
        ShowSelectedWindow(targetWindow);
    }

    public void OpenSelectedWindowOnDoubleClick(GameObject targetWindow)
    {
        if (RegisterDoubleClick(targetWindow))
            ShowSelectedWindow(targetWindow);
    }

    private void ShowSelectedWindow(GameObject targetWindow)
    {
        if (targetWindow == null)
            return;

        targetWindow.SetActive(true);
        FocusWindow(targetWindow);
    }

    public void FocusWindow(GameObject targetWindow)
    {
        if (targetWindow == null)
            return;

        GameObject desktopWindow = FindDesktopWindow(targetWindow);
        if (desktopWindow == null)
            return;

        desktopWindow.SetActive(true);
        desktopWindow.transform.SetAsLastSibling();
        currentSelectedWindow = desktopWindow;
    }

    public void CloseWindow()
    {
        CloseSelectedWindow(windowPanel);
    }

    public void CloseSelectedWindow(GameObject targetWindow)
    {
        if (targetWindow == null)
            return;

        if (targetWindow.activeInHierarchy)
            EmployeeHandbook.Feishu.FeishuSfxPlayer.PlayCloseClickSfx();

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
            if (ShouldKeepVisible(child))
            {
                child.gameObject.SetActive(true);
                continue;
            }

            child.gameObject.SetActive(false);
        }
    }

    private void InstallDesktopWindowFocusTargets()
    {
        foreach (Transform child in transform)
        {
            if (ShouldKeepVisible(child))
                continue;

            DesktopWindowFocusTarget.BindWindow(child.gameObject, this);
        }
    }

    private bool ShouldKeepVisible(Transform child)
    {
        if (child == null)
            return false;

        string childName = child.name.Trim();
        if (childName == DesktopButtonsName || childName == DailyTasksName || childName.Contains(DailyTasksName))
            return true;

        if (alwaysVisibleObjects == null)
            return false;

        for (int i = 0; i < alwaysVisibleObjects.Length; i++)
        {
            GameObject alwaysVisibleObject = alwaysVisibleObjects[i];
            if (alwaysVisibleObject == null)
                continue;

            if (alwaysVisibleObject == child.gameObject || alwaysVisibleObject.transform.IsChildOf(child))
                return true;
        }

        return false;
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

/// <summary>
/// Brings its owning desktop window to the front when any UI element inside it is clicked.
/// This component is installed at runtime by WindowController.
/// </summary>
public class DesktopWindowFocusTarget : MonoBehaviour, IPointerDownHandler
{
    private WindowController windowController;
    private GameObject desktopWindow;

    public static void BindWindow(GameObject windowRoot, WindowController controller)
    {
        if (windowRoot == null || controller == null)
            return;

        Transform[] children = windowRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            DesktopWindowFocusTarget focusTarget = children[i].GetComponent<DesktopWindowFocusTarget>();
            if (focusTarget == null)
                focusTarget = children[i].gameObject.AddComponent<DesktopWindowFocusTarget>();

            focusTarget.windowController = controller;
            focusTarget.desktopWindow = windowRoot;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (windowController == null || desktopWindow == null)
            return;

        windowController.FocusWindow(desktopWindow);
    }
}
