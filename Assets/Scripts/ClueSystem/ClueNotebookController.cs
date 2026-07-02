using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EmployeeHandbook.ClueSystem
{
    /// <summary>
    /// Controls the clue notebook window opened from the bookshelf button.
    /// Attach this to an object that stays active, then assign Notebook Panel.
    /// </summary>
    public class ClueNotebookController : MonoBehaviour
    {
        private const string DefaultNotebookName = "Notebook";

        [SerializeField] private GameObject notebookPanel;
        [SerializeField] private RectTransform notebookClickArea;
        [SerializeField] private MonoBehaviour pageController;
        [SerializeField] private bool hideNotebookOnStart = true;
        [SerializeField] private bool bringToFrontOnOpen = true;
        [SerializeField] private bool closeOnOutsideClick = true;
        [SerializeField] private bool resetPagesOnOpen = true;

        private int lastOpenedFrame = -1;
        private bool openedByBrowser;

        private void Start()
        {
            EnsureNotebookPanel();

            if (hideNotebookOnStart && notebookPanel != null)
                notebookPanel.SetActive(false);
        }

        private void Update()
        {
            if (!closeOnOutsideClick || notebookPanel == null || !notebookPanel.activeSelf)
                return;

            if (openedByBrowser)
                return;

            if (Time.frameCount == lastOpenedFrame)
                return;

            if (!WasPointerPressed(out Vector2 screenPosition))
                return;

            RectTransform clickArea = GetNotebookClickArea();
            if (clickArea == null)
                return;

            Camera eventCamera = GetEventCamera(clickArea);
            if (!RectTransformUtility.RectangleContainsScreenPoint(clickArea, screenPosition, eventCamera))
                CloseNotebook();
        }

        public void OpenNotebook()
        {
            openedByBrowser = false;
            SetNotebookVisible(true);
        }

        public void OpenNotebookFromBrowser()
        {
            openedByBrowser = true;
            SetNotebookVisible(true);
        }

        public void OpenNotebook(GameObject targetNotebook)
        {
            notebookPanel = targetNotebook;
            OpenNotebook();
        }

        public void CloseNotebook()
        {
            if (openedByBrowser)
                return;

            SetNotebookVisible(false);
        }

        public void CloseNotebookFromBrowser()
        {
            openedByBrowser = false;
            SetNotebookVisible(false);
        }

        public void ForceCloseNotebook()
        {
            openedByBrowser = false;
            SetNotebookVisible(false);
        }

        public void ToggleNotebook()
        {
            if (!EnsureNotebookPanel())
                return;

            SetNotebookVisible(!notebookPanel.activeSelf);
        }

        private void SetNotebookVisible(bool visible)
        {
            if (!EnsureNotebookPanel())
                return;

            notebookPanel.SetActive(visible);

            if (visible)
            {
                lastOpenedFrame = Time.frameCount;

                if (resetPagesOnOpen)
                    ResetNotebookPages();
            }

            if (visible && bringToFrontOnOpen)
                notebookPanel.transform.SetAsLastSibling();
        }

        private bool EnsureNotebookPanel()
        {
            if (notebookPanel != null)
                return true;

            Transform notebookTransform = FindChildByName(transform, DefaultNotebookName);
            if (notebookTransform != null)
            {
                notebookPanel = notebookTransform.gameObject;
                return true;
            }

            GameObject activeNotebook = GameObject.Find(DefaultNotebookName);
            if (activeNotebook != null)
            {
                notebookPanel = activeNotebook;
                return true;
            }

            Debug.LogWarning("ClueNotebookController 找不到 Notebook 面板，请在 Inspector 里拖入 Notebook Panel。", this);
            return false;
        }

        private void ResetNotebookPages()
        {
            if (pageController == null)
                pageController = FindPageController(transform);

            if (pageController == null && notebookPanel != null)
                pageController = FindPageController(notebookPanel.transform);

            if (pageController != null)
                pageController.SendMessage("ResetToFirstSpread", SendMessageOptions.DontRequireReceiver);
        }

        private RectTransform GetNotebookClickArea()
        {
            if (notebookClickArea != null)
                return notebookClickArea;

            if (notebookPanel == null)
                return null;

            return notebookPanel.GetComponent<RectTransform>();
        }

        private static bool WasPointerPressed(out Vector2 screenPosition)
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPosition = Mouse.current.position.ReadValue();
                return true;
            }

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButtonDown(0))
            {
                screenPosition = Input.mousePosition;
                return true;
            }

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    screenPosition = touch.position;
                    return true;
                }
            }
#endif

            screenPosition = Vector2.zero;
            return false;
        }

        private static Camera GetEventCamera(RectTransform rectTransform)
        {
            Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            return canvas.worldCamera;
        }

        private static MonoBehaviour FindPageController(Transform root)
        {
            if (root == null)
                return null;

            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] != null && behaviours[i].GetType().Name == "ClueNotebookPageController")
                    return behaviours[i];
            }

            return null;
        }

        private static Transform FindChildByName(Transform root, string childName)
        {
            if (root == null)
                return null;

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != root && children[i].name == childName)
                    return children[i];
            }

            return null;
        }
    }
}
