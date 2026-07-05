using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmployeeHandbook.DailyTasks
{
    public class PromotionCutsceneController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button continueButton;
        [SerializeField] private PromotionMessage[] messages =
        {
            new PromotionMessage
            {
                triggerDay = 5,
                message = "职位调整通知\n\n魏艾：\n鉴于你在试用期内表现稳定，现调整为：\nLv2 初级台账专员\n\n请继续保持当前工作节奏。"
            },
            new PromotionMessage
            {
                triggerDay = 9,
                message = "职位调整通知\n\n魏艾：\n你的权限等级已更新。\n现调整为：\nLv3 中级台账专员\n\n更多系统内容已开放。"
            },
            new PromotionMessage
            {
                triggerDay = 13,
                message = "职位调整通知\n\n魏艾：\n根据近期工作记录，现调整为：\nLv4 资深台账专员\n\n请继续完成后续归档相关工作。"
            }
        };

        [Header("Timing")]
        [SerializeField] private float fadeInDuration = 0.2f;
        [SerializeField] private float delayBeforeTyping = 1f;
        [Tooltip("每个字出现的间隔，越小打字越快。")]
        [SerializeField] private float characterInterval = 0.035f;
        [SerializeField] private float fadeOutDuration = 0.2f;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip typingAudio;
        [SerializeField] private bool loopTypingAudio = true;
        [SerializeField] private bool stopTypingAudioWhenTextComplete = true;

        private Coroutine playRoutine;
        private Action finishCallback;
        private bool waitingForClick;
        private bool startCovered;
        private PromotionMessage preparedMessage;

        private void Awake()
        {
            EnsureReferences();
            HideImmediately();

            if (continueButton != null)
                continueButton.onClick.AddListener(Continue);
        }

        public bool ShouldPlayForDay(int day)
        {
            return FindMessage(day) != null;
        }

        public void Play(int day, Action onFinished)
        {
            PlayInternal(day, onFinished, false);
        }

        public void PlayCovered(int day, Action onFinished)
        {
            PlayInternal(day, onFinished, true);
        }

        public IEnumerator FadeInCovered(int day, Action onFinished)
        {
            PromotionMessage message = FindMessage(day);
            if (message == null)
            {
                onFinished?.Invoke();
                yield break;
            }

            if (playRoutine != null)
                StopCoroutine(playRoutine);

            finishCallback = onFinished;
            preparedMessage = message;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            EnsureReferences();
            PrepareVisibleState();
            yield return Fade(0f, 1f, fadeInDuration);

            playRoutine = StartCoroutine(TypePreparedMessage());
        }

        private void PlayInternal(int day, Action onFinished, bool covered)
        {
            PromotionMessage message = FindMessage(day);
            if (message == null)
            {
                onFinished?.Invoke();
                return;
            }

            if (playRoutine != null)
                StopCoroutine(playRoutine);

            finishCallback = onFinished;
            startCovered = covered;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            playRoutine = StartCoroutine(PlayRoutine(message.message));
        }

        private IEnumerator PlayRoutine(string message)
        {
            EnsureReferences();

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            PrepareVisibleState();

            if (startCovered)
            {
                yield return Fade(0f, 1f, fadeInDuration);
            }
            else
            {
                yield return Fade(0f, 1f, fadeInDuration);
            }

            yield return TypeMessage(message);
        }

        private IEnumerator TypePreparedMessage()
        {
            if (preparedMessage == null)
                yield break;

            yield return TypeMessage(preparedMessage.message);
        }

        private IEnumerator TypeMessage(string message)
        {
            if (delayBeforeTyping > 0f)
                yield return new WaitForSecondsRealtime(delayBeforeTyping);

            if (messageText != null)
            {
                PlayTypingAudio();

                for (int i = 0; i <= message.Length; i++)
                {
                    messageText.text = message.Substring(0, i);
                    if (characterInterval > 0f)
                        yield return new WaitForSecondsRealtime(characterInterval);
                    else
                        yield return null;
                }

                if (stopTypingAudioWhenTextComplete)
                    StopTypingAudio();
            }

            waitingForClick = true;

            if (continueButton != null)
                continueButton.interactable = true;
        }

        private void PrepareVisibleState()
        {
            waitingForClick = false;

            if (messageText != null)
                messageText.text = string.Empty;

            if (continueButton != null)
                continueButton.interactable = false;

            if (canvasGroup != null)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }

        private void Continue()
        {
            if (!waitingForClick)
                return;

            waitingForClick = false;

            if (continueButton != null)
                continueButton.interactable = false;

            StopTypingAudio();

            if (playRoutine != null)
                StopCoroutine(playRoutine);

            playRoutine = StartCoroutine(FinishRoutine());
        }

        private IEnumerator FinishRoutine()
        {
            yield return Fade(1f, 0f, fadeOutDuration);
            HideImmediately();

            Action callback = finishCallback;
            finishCallback = null;
            playRoutine = null;
            callback?.Invoke();
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            if (canvasGroup == null)
                yield break;

            if (duration <= 0f)
            {
                canvasGroup.alpha = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                canvasGroup.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }

            canvasGroup.alpha = to;
        }

        private void HideImmediately()
        {
            EnsureReferences();

            waitingForClick = false;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (continueButton != null)
                continueButton.interactable = false;

            StopTypingAudio();
            gameObject.SetActive(false);
        }

        private void PlayTypingAudio()
        {
            if (typingAudio == null)
                return;

            EnsureReferences();
            if (audioSource == null)
                return;

            audioSource.Stop();
            audioSource.clip = typingAudio;
            audioSource.loop = loopTypingAudio;
            audioSource.Play();
        }

        private void StopTypingAudio()
        {
            if (audioSource != null && audioSource.isPlaying)
                audioSource.Stop();
        }

        private PromotionMessage FindMessage(int day)
        {
            if (messages == null)
                return null;

            for (int i = 0; i < messages.Length; i++)
            {
                if (messages[i] != null && messages[i].triggerDay == day)
                    return messages[i];
            }

            return null;
        }

        private void EnsureReferences()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (messageText == null)
                messageText = GetComponentInChildren<TMP_Text>(true);

            if (continueButton == null)
                continueButton = GetComponentInChildren<Button>(true);

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        [Serializable]
        private class PromotionMessage
        {
            public int triggerDay;
            [TextArea(3, 8)] public string message;
        }
    }
}
