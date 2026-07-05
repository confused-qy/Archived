using System;
using System.Collections;
using UnityEngine;

namespace EmployeeHandbook.DailyTasks
{
    /// <summary>
    /// 一天结束后的日历过场：遮罩淡入、日历放大、划掉完成日期、淡出。
    /// </summary>
    public class DayEndTransitionController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private CanvasGroup rootCanvasGroup;
        [SerializeField] private RectTransform calendarRoot;
        [SerializeField] private Transform slashRoot;
        [SerializeField] private RectTransform[] slashLines = new RectTransform[20];

        [Header("Timing")]
        [SerializeField] private float fadeInDuration = 0.25f;
        [SerializeField] private float calendarScaleDuration = 0.35f;
        [SerializeField] private float slashDuration = 0.35f;
        [SerializeField] private float holdDuration = 0.45f;
        [SerializeField] private float fadeOutDuration = 0.25f;

        [Header("Scale")]
        [SerializeField] private Vector3 hiddenCalendarScale = new Vector3(0.65f, 0.65f, 1f);
        [SerializeField] private Vector3 shownCalendarScale = Vector3.one;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip showSound;
        [SerializeField] private AudioClip slashSound;
        [SerializeField] private AudioClip hideSound;

        private Coroutine playRoutine;

        private void Awake()
        {
            AutoFindReferences();
            CacheSlashLines();
            HideImmediately();
        }

        public void Play(int completedDay)
        {
            Play(completedDay, null);
        }

        public void Play(int completedDay, Action onFinished)
        {
            Play(completedDay, null, onFinished);
        }

        public void Play(int completedDay, Action beforeFadeOut, Action onFinished)
        {
            Play(completedDay, beforeFadeOut != null ? () => InvokeActionRoutine(beforeFadeOut) : null, onFinished);
        }

        public void Play(int completedDay, Func<IEnumerator> beforeFadeOutRoutine, Action onFinished)
        {
            if (completedDay < 1 || completedDay > slashLines.Length)
            {
                Debug.LogWarning("DayEndTransitionController 收到无效日期：" + completedDay, this);
                onFinished?.Invoke();
                return;
            }

            if (playRoutine != null)
                StopCoroutine(playRoutine);

            playRoutine = StartCoroutine(PlayRoutine(completedDay, beforeFadeOutRoutine, onFinished));
        }

        public void HideImmediately()
        {
            EnsureCanvasGroup();

            if (rootCanvasGroup != null)
            {
                rootCanvasGroup.alpha = 0f;
                rootCanvasGroup.interactable = false;
                rootCanvasGroup.blocksRaycasts = false;
            }

            if (calendarRoot != null)
                calendarRoot.localScale = hiddenCalendarScale;

            ResetAllSlashScales();
        }

        private IEnumerator PlayRoutine(int completedDay, Func<IEnumerator> beforeFadeOutRoutine, Action onFinished)
        {
            EnsureCanvasGroup();
            CacheSlashLines();

            RectTransform slash = GetSlashLine(completedDay);
            if (slash == null)
            {
                Debug.LogWarning("DayEndTransitionController 找不到 Slash" + completedDay + "。", this);
                onFinished?.Invoke();
                yield break;
            }

            PrepareSlashScales(completedDay);

            if (rootCanvasGroup != null)
            {
                rootCanvasGroup.interactable = true;
                rootCanvasGroup.blocksRaycasts = true;
            }

            PlayOneShot(showSound);

            float introDuration = Mathf.Max(fadeInDuration, calendarScaleDuration);
            float elapsed = 0f;
            while (elapsed < introDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float fadeT = fadeInDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / fadeInDuration);
                float scaleT = calendarScaleDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / calendarScaleDuration);
                fadeT = EaseOutCubic(fadeT);
                scaleT = EaseOutCubic(scaleT);

                if (rootCanvasGroup != null)
                    rootCanvasGroup.alpha = fadeT;

                if (calendarRoot != null)
                    calendarRoot.localScale = Vector3.LerpUnclamped(hiddenCalendarScale, shownCalendarScale, scaleT);

                yield return null;
            }

            if (rootCanvasGroup != null)
                rootCanvasGroup.alpha = 1f;

            if (calendarRoot != null)
                calendarRoot.localScale = shownCalendarScale;

            PlayOneShot(slashSound);

            elapsed = 0f;
            while (elapsed < slashDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = slashDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / slashDuration);
                SetSlashScaleX(slash, EaseOutCubic(t));
                yield return null;
            }

            SetSlashScaleX(slash, 1f);

            if (holdDuration > 0f)
                yield return new WaitForSecondsRealtime(holdDuration);

            if (beforeFadeOutRoutine != null)
                yield return beforeFadeOutRoutine();

            PlayOneShot(hideSound);

            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = fadeOutDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / fadeOutDuration);
                t = EaseOutCubic(t);

                if (rootCanvasGroup != null)
                    rootCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

                if (calendarRoot != null)
                    calendarRoot.localScale = Vector3.LerpUnclamped(shownCalendarScale, hiddenCalendarScale, t);

                yield return null;
            }

            HideImmediately();
            playRoutine = null;
            onFinished?.Invoke();
        }

        private IEnumerator InvokeActionRoutine(Action action)
        {
            action?.Invoke();
            yield break;
        }

        private RectTransform GetSlashLine(int day)
        {
            int index = day - 1;
            if (index < 0 || index >= slashLines.Length)
                return null;

            return slashLines[index];
        }

        private void CacheSlashLines()
        {
            if (slashLines == null || slashLines.Length != 20)
                slashLines = new RectTransform[20];

            if (slashRoot == null)
                return;

            for (int day = 1; day <= slashLines.Length; day++)
            {
                int index = day - 1;
                if (slashLines[index] != null)
                    continue;

                Transform slash = slashRoot.Find("Slash" + day);
                if (slash != null)
                    slashLines[index] = slash as RectTransform;
            }
        }

        private void ResetAllSlashScales()
        {
            if (slashLines == null)
                return;

            for (int i = 0; i < slashLines.Length; i++)
            {
                if (slashLines[i] != null)
                    SetSlashScaleX(slashLines[i], 0f);
            }
        }

        private void PrepareSlashScales(int completedDay)
        {
            if (slashLines == null)
                return;

            for (int day = 1; day <= slashLines.Length; day++)
            {
                RectTransform slash = slashLines[day - 1];
                if (slash == null)
                    continue;

                if (day < completedDay)
                    SetSlashScaleX(slash, 1f);
                else
                    SetSlashScaleX(slash, 0f);
            }
        }

        private void SetSlashScaleX(RectTransform slash, float x)
        {
            if (slash == null)
                return;

            Vector3 scale = slash.localScale;
            scale.x = x;
            slash.localScale = scale;
        }

        private void AutoFindReferences()
        {
            if (rootCanvasGroup == null)
                rootCanvasGroup = GetComponent<CanvasGroup>();

            if (calendarRoot == null)
            {
                Transform found = transform.Find("CalendarRoot");
                if (found != null)
                    calendarRoot = found as RectTransform;
            }

            if (slashRoot == null && calendarRoot != null)
                slashRoot = calendarRoot.Find("SlashRoot");

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        private void EnsureCanvasGroup()
        {
            if (rootCanvasGroup != null)
                return;

            rootCanvasGroup = GetComponent<CanvasGroup>();
            if (rootCanvasGroup == null)
                rootCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (audioSource == null || clip == null)
                return;

            audioSource.PlayOneShot(clip);
        }

        private float EaseOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - Mathf.Pow(1f - t, 3f);
        }
    }
}
