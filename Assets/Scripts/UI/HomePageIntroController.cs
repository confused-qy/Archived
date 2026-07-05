using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EmployeeHandbook.DailyTasks
{
    /// <summary>
    /// HomePage 内的新游戏开场文字：黑屏淡入、打字、点击继续进入 MainPage。
    /// </summary>
    public class HomePageIntroController : MonoBehaviour, IPointerClickHandler
    {
        [Header("Scene")]
        [SerializeField] private string mainSceneName = "MainPage";

        [Header("UI")]
        [SerializeField] private GameObject introPanel;
        [SerializeField] private CanvasGroup introCanvasGroup;
        [SerializeField] private Text introText;
        [SerializeField] private TMP_Text introTmpText;

        [Header("Text")]
        [TextArea(4, 10)]
        [SerializeField] private string introContent =
            "今天是 3 月 1 日。\n\n你叫魏艾。\n\n这是你入职 MonkeyAI 的第一天。\n\n请阅读员工手册，完成每日工作，并遵守所有公司规定。";
        [SerializeField] private float delayBeforeTyping = 1f;
        [SerializeField] private float charactersPerSecond = 18f;

        [Header("Fade")]
        [SerializeField] private float fadeInDuration = 0.35f;

        [Header("Typing Sound")]
        [SerializeField] private AudioSource typeAudioSource;
        [SerializeField] private AudioClip typeSound;
        [Range(0f, 1f)]
        [SerializeField] private float typeSoundVolume = 0.7f;

        private Coroutine introRoutine;
        private bool isPlaying;
        private bool finishedTyping;
        private bool waitingForFinalClick;

        private void Awake()
        {
            EnsureReferences();
            HideImmediately();
        }

        public void PlayNewGameIntro()
        {
            if (isPlaying)
                return;

            GameLaunchState.RequestNewGame();

            if (introRoutine != null)
                StopCoroutine(introRoutine);

            introRoutine = StartCoroutine(PlayIntroRoutine());
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            HandleClick();
        }

        public void HandleIntroClick()
        {
            HandleClick();
        }

        private void HandleClick()
        {
            if (!isPlaying)
                return;

            if (!finishedTyping)
            {
                finishedTyping = true;
                SetIntroText(introContent);
                StopTypeSound();
                return;
            }

            if (waitingForFinalClick)
            {
                waitingForFinalClick = false;
                StartCoroutine(ExitIntroAndLoadMain());
            }
        }

        private IEnumerator PlayIntroRoutine()
        {
            isPlaying = true;
            finishedTyping = false;
            waitingForFinalClick = false;
            SetIntroText(string.Empty);

            if (introPanel != null)
                introPanel.SetActive(true);

            yield return FadeCanvas(0f, 1f, fadeInDuration);
            yield return TypeText();

            finishedTyping = true;
            SetIntroText(introContent);
            waitingForFinalClick = true;
            introRoutine = null;
        }

        private IEnumerator TypeText()
        {
            if (delayBeforeTyping > 0f)
                yield return new WaitForSecondsRealtime(delayBeforeTyping);

            if (finishedTyping)
                yield break;

            if (charactersPerSecond <= 0f)
            {
                SetIntroText(introContent);
                yield break;
            }

            StartTypeSound();
            float delay = 1f / charactersPerSecond;
            for (int i = 0; i <= introContent.Length; i++)
            {
                if (finishedTyping)
                {
                    StopTypeSound();
                    yield break;
                }

                SetIntroText(introContent.Substring(0, i));
                yield return new WaitForSecondsRealtime(delay);
            }

            StopTypeSound();
        }

        private void StartTypeSound()
        {
            if (typeAudioSource == null || typeSound == null)
                return;

            typeAudioSource.clip = typeSound;
            typeAudioSource.volume = typeSoundVolume;
            typeAudioSource.loop = true;
            typeAudioSource.Play();
        }

        private void StopTypeSound()
        {
            if (typeAudioSource == null)
                return;

            if (typeAudioSource.isPlaying)
                typeAudioSource.Stop();
        }

        private IEnumerator ExitIntroAndLoadMain()
        {
            StopTypeSound();
            SetCanvasAlpha(1f);
            SetIntroText(string.Empty);
            isPlaying = false;
            yield return null;

            if (string.IsNullOrWhiteSpace(mainSceneName))
            {
                Debug.LogWarning("HomePageIntroController 没有设置 Main Scene Name。", this);
                yield break;
            }

            SceneTransitionController.LoadSceneFromBlack(mainSceneName);
        }

        private IEnumerator FadeCanvas(float fromAlpha, float toAlpha, float duration)
        {
            EnsureReferences();

            if (duration <= 0f)
            {
                SetCanvasAlpha(toAlpha);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = 1f - Mathf.Pow(1f - t, 3f);
                SetCanvasAlpha(Mathf.Lerp(fromAlpha, toAlpha, t));
                yield return null;
            }

            SetCanvasAlpha(toAlpha);
        }

        private void HideImmediately()
        {
            SetCanvasAlpha(0f);
            SetIntroText(string.Empty);

            if (introPanel != null)
                introPanel.SetActive(false);
        }

        private void SetCanvasAlpha(float alpha)
        {
            if (introCanvasGroup == null)
                return;

            introCanvasGroup.alpha = alpha;
            introCanvasGroup.interactable = alpha > 0.01f;
            introCanvasGroup.blocksRaycasts = alpha > 0.01f;
        }

        private void SetIntroText(string value)
        {
            if (introText != null)
                introText.text = value;

            if (introTmpText != null)
                introTmpText.text = value;
        }

        private void EnsureReferences()
        {
            if (introPanel == null)
                introPanel = gameObject;

            if (introCanvasGroup == null && introPanel != null)
            {
                introCanvasGroup = introPanel.GetComponent<CanvasGroup>();
                if (introCanvasGroup == null)
                    introCanvasGroup = introPanel.AddComponent<CanvasGroup>();
            }

            if (introTmpText == null && introPanel != null)
                introTmpText = introPanel.GetComponentInChildren<TMP_Text>(true);

            if (introText == null && introPanel != null)
                introText = introPanel.GetComponentInChildren<Text>(true);
        }
    }
}
