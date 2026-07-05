using System.Collections;
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
        [SerializeField] private bool animateOnOpen = true;
        [SerializeField] private float openAnimationDuration = 0.22f;
        [SerializeField] private float openStartYOffset = -180f;

        private int lastOpenedFrame = -1;
        private bool openedByBrowser;
        private RectTransform notebookRectTransform;
        private CanvasGroup notebookCanvasGroup;
        private Vector2 shownAnchoredPosition;
        private Coroutine openAnimationRoutine;

        private void Start()
        {
            EnsureNotebookPanel();
            CacheAnimationReferences();

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

            if (!visible)
            {
                StopOpenAnimation();
                notebookPanel.SetActive(false);
                return;
            }

            notebookPanel.SetActive(true);

            lastOpenedFrame = Time.frameCount;

            if (resetPagesOnOpen)
                ResetNotebookPages();

            if (bringToFrontOnOpen)
                notebookPanel.transform.SetAsLastSibling();

            PlayOpenAnimation();
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

        private void CacheAnimationReferences()
        {
            if (notebookPanel == null)
                return;

            if (notebookRectTransform == null)
                notebookRectTransform = notebookPanel.GetComponent<RectTransform>();

            if (notebookRectTransform != null && shownAnchoredPosition == Vector2.zero)
                shownAnchoredPosition = notebookRectTransform.anchoredPosition;

            if (notebookCanvasGroup == null)
            {
                notebookCanvasGroup = notebookPanel.GetComponent<CanvasGroup>();
                if (notebookCanvasGroup == null)
                    notebookCanvasGroup = notebookPanel.AddComponent<CanvasGroup>();
            }
        }

        private void PlayOpenAnimation()
        {
            CacheAnimationReferences();

            if (!animateOnOpen || notebookRectTransform == null)
            {
                if (notebookCanvasGroup != null)
                    notebookCanvasGroup.alpha = 1f;
                return;
            }

            StopOpenAnimation();
            openAnimationRoutine = StartCoroutine(OpenAnimationRoutine());
        }

        private IEnumerator OpenAnimationRoutine()
        {
            Vector2 hiddenPosition = shownAnchoredPosition + new Vector2(0f, openStartYOffset);

            if (notebookCanvasGroup != null)
            {
                notebookCanvasGroup.alpha = 0f;
                notebookCanvasGroup.interactable = false;
                notebookCanvasGroup.blocksRaycasts = false;
            }

            notebookRectTransform.anchoredPosition = hiddenPosition;

            float elapsed = 0f;
            while (elapsed < openAnimationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = openAnimationDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / openAnimationDuration);
                t = EaseOutCubic(t);

                notebookRectTransform.anchoredPosition = Vector2.LerpUnclamped(hiddenPosition, shownAnchoredPosition, t);

                if (notebookCanvasGroup != null)
                    notebookCanvasGroup.alpha = t;

                yield return null;
            }

            notebookRectTransform.anchoredPosition = shownAnchoredPosition;

            if (notebookCanvasGroup != null)
            {
                notebookCanvasGroup.alpha = 1f;
                notebookCanvasGroup.interactable = true;
                notebookCanvasGroup.blocksRaycasts = true;
            }

            openAnimationRoutine = null;
        }

        private void StopOpenAnimation()
        {
            if (openAnimationRoutine == null)
                return;

            StopCoroutine(openAnimationRoutine);
            openAnimationRoutine = null;

            if (notebookRectTransform != null)
                notebookRectTransform.anchoredPosition = shownAnchoredPosition;

            if (notebookCanvasGroup != null)
            {
                notebookCanvasGroup.alpha = 1f;
                notebookCanvasGroup.interactable = true;
                notebookCanvasGroup.blocksRaycasts = true;
            }
        }

        private static float EaseOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - Mathf.Pow(1f - t, 3f);
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
