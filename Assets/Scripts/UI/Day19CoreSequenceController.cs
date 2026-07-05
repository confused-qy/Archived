using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Video;

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
        [Tooltip("只放结尾视频画面的物体，例如 VideoRawImage。GameOverRoot/CreditsRoot 如果在 EndingRoot 下面，不要拖到这里。")]
        [SerializeField] private GameObject endingVideoDisplayRoot;
        [SerializeField] private CanvasGroup endingCanvasGroup;
        [SerializeField] private VideoPlayer endingVideoPlayer;
        [SerializeField] private VideoClip exposeEndingVideo;
        [SerializeField] private VideoClip closeInvestigationEndingVideo;
        [SerializeField] private VideoClip reformEndingVideo;
        [SerializeField] private VideoClip fallbackEndingVideo;
        [SerializeField] private float endingVideoFadeInDuration = 0.25f;
        [SerializeField] private float endingVideoFadeOutDuration = 0.5f;
        [SerializeField] private GameObject[] exposeEndingSlides;
        [SerializeField] private GameObject[] closeInvestigationEndingSlides;
        [SerializeField] private GameObject[] reformEndingSlides;
        [SerializeField] private GameObject[] fallbackEndingSlides;
        [SerializeField] private float endingSlideDuration = 1.5f;

        [Header("Game Over Text")]
        [SerializeField] private GameObject gameOverRoot;
        [SerializeField] private CanvasGroup gameOverCanvasGroup;
        [SerializeField] private Text gameOverText;
        [SerializeField] private TMP_Text gameOverTmpText;
        [SerializeField] private string gameOverMessage = "游戏结束，感谢你的游玩。";
        [SerializeField] private float gameOverFadeInDuration = 0.25f;
        [SerializeField] private float gameOverDelayBeforeTyping = 0.6f;
        [SerializeField] private float gameOverCharacterInterval = 0.06f;
        [SerializeField] private float gameOverHoldDuration = 1f;
        [SerializeField] private AudioSource gameOverTypingAudioSource;

        [Header("Credits")]
        [SerializeField] private GameObject creditsRoot;
        [SerializeField] private CanvasGroup creditsCanvasGroup;
        [SerializeField] private ScrollRect creditsScrollRect;
        [SerializeField] private RectTransform creditsContent;
        [SerializeField] private float creditsStartY = -520f;
        [SerializeField] private float creditsEndY = 1200f;
        [SerializeField] private float creditsDuration = 22f;
        [SerializeField] private float creditsFadeInDuration = 0.35f;

        [Header("Events")]
        [SerializeField] private UnityEvent onSequenceStarted;
        [SerializeField] private UnityEvent onChoiceMade;
        [SerializeField] private UnityEvent onEndingFinished;

        private Coroutine sequenceRoutine;
        private bool subscribed;
        private bool hasTriggered;
        private bool creditsScrollRectOriginalEnabled;
        private bool creditsScrollRectWasPrepared;
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

            VideoClip clip = GetEndingVideo(endingId);
            if (clip != null)
                yield return PlayEndingVideo(clip);
            else
            {
                GameObject[] slides = GetEndingSlides(endingId);
                yield return PlaySlides(endingRoot, slides, endingSlideDuration);
            }

            yield return PlayGameOverText(creditsRoot == null);
            yield return PlayCredits();
            onEndingFinished?.Invoke();
        }

        private VideoClip GetEndingVideo(string endingId)
        {
            if (endingId == "Expose" && exposeEndingVideo != null)
                return exposeEndingVideo;

            if (endingId == "CloseInvestigation" && closeInvestigationEndingVideo != null)
                return closeInvestigationEndingVideo;

            if (endingId == "Reform" && reformEndingVideo != null)
                return reformEndingVideo;

            return fallbackEndingVideo;
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

        private IEnumerator PlayEndingVideo(VideoClip clip)
        {
            if (endingRoot != null)
                endingRoot.SetActive(true);

            if (endingVideoDisplayRoot != null)
                endingVideoDisplayRoot.SetActive(true);

            if (endingCanvasGroup == null && endingRoot != null)
                endingCanvasGroup = GetOrAddCanvasGroup(endingRoot);

            if (endingCanvasGroup != null)
            {
                endingCanvasGroup.alpha = 0f;
                endingCanvasGroup.interactable = false;
                endingCanvasGroup.blocksRaycasts = false;
            }

            if (endingVideoPlayer == null)
                yield break;

            endingVideoPlayer.Stop();
            endingVideoPlayer.clip = clip;
            endingVideoPlayer.isLooping = false;
            endingVideoPlayer.Prepare();

            while (!endingVideoPlayer.isPrepared)
                yield return null;

            endingVideoPlayer.Play();

            if (endingCanvasGroup != null)
                yield return FadeCanvasGroup(endingCanvasGroup, 0f, 1f, endingVideoFadeInDuration);

            while (endingVideoPlayer != null && endingVideoPlayer.isPlaying)
                yield return null;

            if (endingCanvasGroup != null)
                yield return FadeCanvasGroup(endingCanvasGroup, endingCanvasGroup.alpha, 0f, endingVideoFadeOutDuration);

            if (endingVideoPlayer != null)
            {
                endingVideoPlayer.Stop();
                if (endingVideoPlayer.targetTexture != null)
                    endingVideoPlayer.targetTexture.Release();
            }

            if (endingVideoDisplayRoot != null)
                endingVideoDisplayRoot.SetActive(false);

            if (endingRoot != null && !HasPostEndingUiUnderEndingRoot())
                endingRoot.SetActive(false);
            else if (endingCanvasGroup != null)
                endingCanvasGroup.alpha = 1f;
        }

        private IEnumerator PlayGameOverText(bool fadeOutAfterHold)
        {
            if (gameOverRoot == null)
                yield break;

            EnsureEndingParentVisible(gameOverRoot);
            if (endingVideoDisplayRoot != null)
                endingVideoDisplayRoot.SetActive(false);

            gameOverRoot.SetActive(true);

            if (gameOverCanvasGroup == null)
                gameOverCanvasGroup = GetOrAddCanvasGroup(gameOverRoot);

            SetGameOverText(string.Empty);
            yield return FadeCanvasGroup(gameOverCanvasGroup, 0f, 1f, gameOverFadeInDuration);

            if (gameOverDelayBeforeTyping > 0f)
                yield return new WaitForSecondsRealtime(gameOverDelayBeforeTyping);

            PlayGameOverTypingAudio();

            if (gameOverCharacterInterval <= 0f)
                SetGameOverText(gameOverMessage);
            else
            {
                for (int i = 0; i <= gameOverMessage.Length; i++)
                {
                    SetGameOverText(gameOverMessage.Substring(0, i));
                    yield return new WaitForSecondsRealtime(gameOverCharacterInterval);
                }
            }

            StopGameOverTypingAudio();

            if (gameOverHoldDuration > 0f)
                yield return new WaitForSecondsRealtime(gameOverHoldDuration);

            if (fadeOutAfterHold)
            {
                yield return FadeCanvasGroup(gameOverCanvasGroup, gameOverCanvasGroup.alpha, 0f, gameOverFadeInDuration);
                gameOverRoot.SetActive(false);
            }
        }

        private IEnumerator PlayCredits()
        {
            if (creditsRoot == null)
                yield break;

            EnsureEndingParentVisible(creditsRoot);
            if (endingVideoDisplayRoot != null)
                endingVideoDisplayRoot.SetActive(false);

            if (creditsCanvasGroup == null)
                creditsCanvasGroup = GetOrAddCanvasGroup(creditsRoot);

            creditsCanvasGroup.alpha = 0f;
            creditsCanvasGroup.interactable = false;
            creditsCanvasGroup.blocksRaycasts = false;

            PrepareCreditsScrollRect();

            if (creditsContent != null)
                SetCreditsContentY(creditsStartY);

            creditsRoot.SetActive(true);
            Canvas.ForceUpdateCanvases();

            if (creditsContent != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(creditsContent);

            if (creditsContent != null)
                SetCreditsContentY(creditsStartY);

            Canvas.ForceUpdateCanvases();
            yield return null;

            if (creditsContent != null)
                SetCreditsContentY(creditsStartY);

            yield return FadeCanvasGroup(creditsCanvasGroup, 0f, 1f, creditsFadeInDuration);

            if (gameOverRoot != null && gameOverRoot.activeSelf)
            {
                if (gameOverCanvasGroup != null)
                    gameOverCanvasGroup.alpha = 0f;

                gameOverRoot.SetActive(false);
            }

            float elapsed = 0f;
            while (elapsed < creditsDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = creditsDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / creditsDuration);

                if (creditsContent != null)
                    SetCreditsContentY(Mathf.Lerp(creditsStartY, creditsEndY, t));

                yield return null;
            }

            RestoreCreditsScrollRect();
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
            {
                CanvasGroup group = endingCanvasGroup != null ? endingCanvasGroup : GetOrAddCanvasGroup(endingRoot);
                group.alpha = 0f;
                endingRoot.SetActive(false);
            }

            if (endingVideoDisplayRoot != null)
                endingVideoDisplayRoot.SetActive(false);

            if (gameOverRoot != null)
            {
                CanvasGroup group = gameOverCanvasGroup != null ? gameOverCanvasGroup : GetOrAddCanvasGroup(gameOverRoot);
                group.alpha = 0f;
                gameOverRoot.SetActive(false);
            }

            if (creditsRoot != null)
            {
                CanvasGroup group = creditsCanvasGroup != null ? creditsCanvasGroup : GetOrAddCanvasGroup(creditsRoot);
                group.alpha = 0f;
                creditsRoot.SetActive(false);
            }

            RestoreCreditsScrollRect();

            HideSlides(exposeEndingSlides);
            HideSlides(closeInvestigationEndingSlides);
            HideSlides(reformEndingSlides);
            HideSlides(fallbackEndingSlides);
        }

        private void SetGameOverText(string value)
        {
            if (gameOverText != null)
                gameOverText.text = value;

            if (gameOverTmpText != null)
                gameOverTmpText.text = value;
        }

        private void PlayGameOverTypingAudio()
        {
            if (gameOverTypingAudioSource == null || gameOverTypingAudioSource.clip == null)
                return;

            gameOverTypingAudioSource.Stop();
            gameOverTypingAudioSource.loop = true;
            gameOverTypingAudioSource.Play();
        }

        private void StopGameOverTypingAudio()
        {
            if (gameOverTypingAudioSource == null)
                return;

            gameOverTypingAudioSource.Stop();
        }

        private void SetCreditsContentY(float y)
        {
            if (creditsContent == null)
                return;

            Vector2 position = creditsContent.anchoredPosition;
            position.y = y;
            creditsContent.anchoredPosition = position;
        }

        private void PrepareCreditsScrollRect()
        {
            if (creditsScrollRect == null)
                return;

            creditsScrollRectOriginalEnabled = creditsScrollRect.enabled;
            creditsScrollRectWasPrepared = true;
            creditsScrollRect.StopMovement();
            creditsScrollRect.velocity = Vector2.zero;
            creditsScrollRect.enabled = false;
        }

        private void RestoreCreditsScrollRect()
        {
            if (creditsScrollRect == null || !creditsScrollRectWasPrepared)
                return;

            creditsScrollRect.StopMovement();
            creditsScrollRect.velocity = Vector2.zero;
            creditsScrollRect.enabled = creditsScrollRectOriginalEnabled;
            creditsScrollRectWasPrepared = false;
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

        private void EnsureEndingParentVisible(GameObject child)
        {
            if (endingRoot == null || child == null)
                return;

            if (child == endingRoot || child.transform.IsChildOf(endingRoot.transform))
            {
                endingRoot.SetActive(true);

                if (endingCanvasGroup == null)
                    endingCanvasGroup = GetOrAddCanvasGroup(endingRoot);

                endingCanvasGroup.alpha = 1f;
                endingCanvasGroup.interactable = false;
                endingCanvasGroup.blocksRaycasts = false;
            }
        }

        private bool HasPostEndingUiUnderEndingRoot()
        {
            if (endingRoot == null)
                return false;

            return IsUnderEndingRoot(gameOverRoot) || IsUnderEndingRoot(creditsRoot);
        }

        private bool IsUnderEndingRoot(GameObject target)
        {
            return target != null && (target == endingRoot || target.transform.IsChildOf(endingRoot.transform));
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
