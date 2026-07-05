using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace EmployeeHandbook.DailyTasks
{
    /// <summary>
    /// 第 19 天核心系统过场：关闭桌面、播放 7 张图、显示最终选择、进入第 20 天并播放结尾 CG。
    /// </summary>
    public class Day19CoreSequenceController : MonoBehaviour
    {
        private const int TriggerDay = 19;

        [Header("Trigger")]
        [SerializeField] private float startDelay = 2f;
        [SerializeField] private bool triggerOnlyOnce = true;

        [Header("Desktop Hide")]
        [SerializeField] private Transform[] desktopRoots;
        [SerializeField] private GameObject[] objectsToHide;
        [SerializeField] private GameObject[] keepActiveObjects;

        [Header("Intro Slides")]
        [SerializeField] private GameObject introRoot;
        [SerializeField] private GameObject[] introSlides = new GameObject[7];
        [SerializeField] private float introSlideDuration = 1.2f;
        [SerializeField] private float slideFadeDuration = 0.25f;

        [Header("Choice Panel")]
        [SerializeField] private GameObject choicePanel;
        [SerializeField] private Text coreStatusText;
        [SerializeField] private TMP_Text coreStatusTmpText;
        [SerializeField] private Button exposeButton;
        [SerializeField] private Button closeInvestigationButton;
        [SerializeField] private Button reformButton;
        [SerializeField] private float choiceFadeDuration = 0.25f;

        [Header("Ending CG")]
        [SerializeField] private GameObject endingRoot;
        [SerializeField] private GameObject[] exposeEndingSlides;
        [SerializeField] private GameObject[] closeInvestigationEndingSlides;
        [SerializeField] private GameObject[] reformEndingSlides;
        [SerializeField] private GameObject[] fallbackEndingSlides;
        [SerializeField] private float endingSlideDuration = 1.5f;

        [Header("Events")]
        [SerializeField] private UnityEvent onSequenceStarted;
        [SerializeField] private UnityEvent onChoiceMade;
        [SerializeField] private UnityEvent onEndingFinished;

        private Coroutine sequenceRoutine;
        private bool subscribed;
        private bool hasTriggered;
        private string selectedEndingId = string.Empty;

        public string SelectedEndingId
        {
            get { return selectedEndingId; }
        }

        private void Awake()
        {
            HideSequenceUi();
            BindButtons();
        }

        private void OnEnable()
        {
            Subscribe();
            RefreshForCurrentDay();
        }

        private void Start()
        {
            Subscribe();
            RefreshForCurrentDay();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void RefreshForCurrentDay()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState == null)
                return;

            if (GameManager.Instance.CurrentState.currentDay != TriggerDay)
                return;

            if (triggerOnlyOnce && hasTriggered)
                return;

            if (sequenceRoutine != null)
                return;

            hasTriggered = true;
            sequenceRoutine = StartCoroutine(StartSequenceAfterDelay());
        }

        public void ChooseExpose()
        {
            ChooseEnding("Expose");
        }

        public void ChooseCloseInvestigation()
        {
            ChooseEnding("CloseInvestigation");
        }

        public void ChooseReform()
        {
            ChooseEnding("Reform");
        }

        private IEnumerator StartSequenceAfterDelay()
        {
            if (startDelay > 0f)
                yield return new WaitForSecondsRealtime(startDelay);

            onSequenceStarted?.Invoke();
            HideDesktop();
            yield return PlayIntroSlides();
            yield return ShowChoicePanel();
            sequenceRoutine = null;
        }

        private void ChooseEnding(string endingId)
        {
            if (!string.IsNullOrEmpty(selectedEndingId))
                return;

            selectedEndingId = endingId;
            onChoiceMade?.Invoke();
            StartCoroutine(ChooseEndingRoutine(endingId));
        }

        private IEnumerator ChooseEndingRoutine(string endingId)
        {
            yield return HideChoicePanel();
            yield return EnterDay20AndPlayEnding(endingId);
        }

        private IEnumerator EnterDay20AndPlayEnding(string endingId)
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != null && GameManager.Instance.CurrentState.currentDay == TriggerDay)
                GameManager.Instance.NextDay();

            GameObject[] slides = GetEndingSlides(endingId);
            yield return PlaySlides(endingRoot, slides, endingSlideDuration);
            onEndingFinished?.Invoke();
        }

        private GameObject[] GetEndingSlides(string endingId)
        {
            if (endingId == "Expose" && exposeEndingSlides != null && exposeEndingSlides.Length > 0)
                return exposeEndingSlides;

            if (endingId == "CloseInvestigation" && closeInvestigationEndingSlides != null && closeInvestigationEndingSlides.Length > 0)
                return closeInvestigationEndingSlides;

            if (endingId == "Reform" && reformEndingSlides != null && reformEndingSlides.Length > 0)
                return reformEndingSlides;

            return fallbackEndingSlides;
        }

        private IEnumerator PlaySlides(GameObject root, GameObject[] slides, float slideDuration)
        {
            if (root != null)
                root.SetActive(true);

            HideSlides(slides);

            if (slides == null)
                yield break;

            for (int i = 0; i < slides.Length; i++)
            {
                GameObject slide = slides[i];
                if (slide == null)
                    continue;

                yield return ShowSlide(slide);

                if (slideDuration > 0f)
                    yield return new WaitForSecondsRealtime(slideDuration);

                if (i < slides.Length - 1)
                    yield return HideSlide(slide);
            }
        }

        private IEnumerator PlayIntroSlides()
        {
            if (introRoot != null)
                introRoot.SetActive(true);

            HideSlides(introSlides);

            if (introSlides == null)
                yield break;

            GameObject previousSlide = null;
            for (int i = 0; i < introSlides.Length; i++)
            {
                GameObject slide = introSlides[i];
                if (slide == null)
                    continue;

                if (previousSlide != null)
                    previousSlide.SetActive(false);

                CanvasGroup group = GetOrAddCanvasGroup(slide);
                group.alpha = 1f;
                slide.SetActive(true);
                previousSlide = slide;

                if (introSlideDuration > 0f)
                    yield return new WaitForSecondsRealtime(introSlideDuration);
            }
        }

        private IEnumerator ShowSlide(GameObject slide)
        {
            CanvasGroup group = GetOrAddCanvasGroup(slide);
            slide.SetActive(true);

            if (slideFadeDuration <= 0f)
            {
                group.alpha = 1f;
                yield break;
            }

            group.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < slideFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / slideFadeDuration);
                group.alpha = EaseOutCubic(t);
                yield return null;
            }

            group.alpha = 1f;
        }

        private IEnumerator HideSlide(GameObject slide)
        {
            CanvasGroup group = GetOrAddCanvasGroup(slide);

            if (slideFadeDuration <= 0f)
            {
                slide.SetActive(false);
                yield break;
            }

            float elapsed = 0f;
            float startAlpha = group.alpha;
            while (elapsed < slideFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / slideFadeDuration);
                group.alpha = Mathf.Lerp(startAlpha, 0f, EaseOutCubic(t));
                yield return null;
            }

            group.alpha = 0f;
            slide.SetActive(false);
        }

        private IEnumerator ShowChoicePanel()
        {
            if (choicePanel == null)
                yield break;

            string text = "MonkeyAI Core\n当前状态：系统运行正常。\n\n该操作将改变MonkeyAI的未来，该操作不可撤销。";
            if (coreStatusText != null)
                coreStatusText.text = text;

            if (coreStatusTmpText != null)
                coreStatusTmpText.text = text;

            choicePanel.SetActive(true);
            CanvasGroup group = GetOrAddCanvasGroup(choicePanel);
            group.interactable = false;
            group.blocksRaycasts = false;

            yield return FadeCanvasGroup(group, 0f, 1f, choiceFadeDuration);

            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }

        private IEnumerator HideChoicePanel()
        {
            if (choicePanel == null)
                yield break;

            CanvasGroup group = GetOrAddCanvasGroup(choicePanel);
            group.interactable = false;
            group.blocksRaycasts = false;

            yield return FadeCanvasGroup(group, group.alpha, 0f, choiceFadeDuration);

            group.alpha = 0f;
            choicePanel.SetActive(false);
        }

        private void HideDesktop()
        {
            if (desktopRoots != null)
            {
                for (int i = 0; i < desktopRoots.Length; i++)
                    HideDesktopRootChildren(desktopRoots[i]);
            }

            if (objectsToHide != null)
            {
                for (int i = 0; i < objectsToHide.Length; i++)
                {
                    if (objectsToHide[i] != null && !ShouldKeepActive(objectsToHide[i]))
                        objectsToHide[i].SetActive(false);
                }
            }
        }

        private void HideDesktopRootChildren(Transform root)
        {
            if (root == null)
                return;

            foreach (Transform child in root)
            {
                if (child != null && !ShouldKeepActive(child.gameObject))
                    child.gameObject.SetActive(false);
            }
        }

        private bool ShouldKeepActive(GameObject target)
        {
            if (target == null || keepActiveObjects == null)
                return false;

            for (int i = 0; i < keepActiveObjects.Length; i++)
            {
                GameObject keep = keepActiveObjects[i];
                if (keep == null)
                    continue;

                if (target == keep || target.transform.IsChildOf(keep.transform) || keep.transform.IsChildOf(target.transform))
                    return true;
            }

            return false;
        }

        private void HideSequenceUi()
        {
            if (introRoot != null)
                introRoot.SetActive(false);

            HideSlides(introSlides);

            if (choicePanel != null)
            {
                CanvasGroup group = GetOrAddCanvasGroup(choicePanel);
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
                choicePanel.SetActive(false);
            }

            if (endingRoot != null)
                endingRoot.SetActive(false);

            HideSlides(exposeEndingSlides);
            HideSlides(closeInvestigationEndingSlides);
            HideSlides(reformEndingSlides);
            HideSlides(fallbackEndingSlides);
        }

        private void HideSlides(GameObject[] slides)
        {
            if (slides == null)
                return;

            for (int i = 0; i < slides.Length; i++)
            {
                if (slides[i] == null)
                    continue;

                CanvasGroup group = GetOrAddCanvasGroup(slides[i]);
                group.alpha = 0f;
                slides[i].SetActive(false);
            }
        }

        private CanvasGroup GetOrAddCanvasGroup(GameObject target)
        {
            CanvasGroup group = target.GetComponent<CanvasGroup>();
            if (group == null)
                group = target.AddComponent<CanvasGroup>();

            return group;
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup group, float fromAlpha, float toAlpha, float duration)
        {
            if (group == null)
                yield break;

            if (duration <= 0f)
            {
                group.alpha = toAlpha;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                group.alpha = Mathf.Lerp(fromAlpha, toAlpha, EaseOutCubic(t));
                yield return null;
            }

            group.alpha = toAlpha;
        }

        private void BindButtons()
        {
            if (exposeButton != null)
            {
                exposeButton.onClick.RemoveListener(ChooseExpose);
                exposeButton.onClick.AddListener(ChooseExpose);
            }

            if (closeInvestigationButton != null)
            {
                closeInvestigationButton.onClick.RemoveListener(ChooseCloseInvestigation);
                closeInvestigationButton.onClick.AddListener(ChooseCloseInvestigation);
            }

            if (reformButton != null)
            {
                reformButton.onClick.RemoveListener(ChooseReform);
                reformButton.onClick.AddListener(ChooseReform);
            }
        }

        private void Subscribe()
        {
            if (subscribed || GameManager.Instance == null)
                return;

            GameManager.Instance.StateChanged += RefreshForCurrentDay;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || GameManager.Instance == null)
                return;

            GameManager.Instance.StateChanged -= RefreshForCurrentDay;
            subscribed = false;
        }

        private float EaseOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - Mathf.Pow(1f - t, 3f);
        }
    }
}
