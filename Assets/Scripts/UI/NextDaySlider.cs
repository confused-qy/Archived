using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EmployeeHandbook.DailyTasks
{
    /// <summary>
    /// 松手时判断是否滑到最右；成功则进入下一天，之后滑块自动回到左边。
    /// </summary>
    public class NextDaySlider : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        [SerializeField] private Slider slider;
        [SerializeField, Range(0f, 1f)] private float triggerValue = 0.95f;
        [SerializeField] private float returnDuration = 0.25f;
        [SerializeField] private DayEndTransitionController dayEndTransition;

        [Header("Optional Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip successSound;
        [SerializeField] private AudioClip failSound;
        [SerializeField] private bool playFailSoundWhenReleasedBeforeTrigger;

        private Coroutine returnRoutine;
        private bool isReturning;
        private bool handledThisDrag;
        private float lastSuccessTime = -10f;
        private int dragStartDay = -1;
        private bool waitingForTransition;

        private void Awake()
        {
            if (slider == null)
                slider = GetComponent<Slider>();

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if (dayEndTransition == null)
                dayEndTransition = FindObjectOfType<DayEndTransitionController>();

            ResetSliderImmediate();
        }

        private void OnDisable()
        {
            StopReturnRoutine();
            ResetSliderImmediate();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (waitingForTransition)
                return;

            StopReturnRoutine();
            handledThisDrag = false;
            dragStartDay = GetCurrentDay();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (slider == null || isReturning || handledThisDrag || waitingForTransition)
                return;

            handledThisDrag = true;
            bool reachedEnd = slider.value >= triggerValue;
            bool success = HasDayChangedSinceDragStarted();

            if (!success && reachedEnd && GameManager.Instance != null && GameManager.Instance.CurrentState != null)
            {
                bool canGoNextDay = GameManager.Instance.TaskManager != null && GameManager.Instance.TaskManager.AreTodayRequiredTasksCompleted();

                if (canGoNextDay)
                    success = true;
            }

            if (reachedEnd)
            {
                if (success)
                {
                    lastSuccessTime = Time.unscaledTime;
                    PlayOneShot(successSound);
                    PlayDayEndTransitionThenGoNextDay();
                }
                else if (Time.unscaledTime - lastSuccessTime > 0.2f)
                    PlayOneShot(failSound);
            }
            else if (playFailSoundWhenReleasedBeforeTrigger)
                PlayOneShot(failSound);

            StartReturnToLeft();
        }

        private int GetCurrentDay()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState == null)
                return -1;

            return GameManager.Instance.CurrentState.currentDay;
        }

        private bool HasDayChangedSinceDragStarted()
        {
            int currentDay = GetCurrentDay();
            return dragStartDay > 0 && currentDay > 0 && currentDay != dragStartDay;
        }

        private void PlayDayEndTransitionThenGoNextDay()
        {
            if (HasDayChangedSinceDragStarted())
            {
                PlayTransitionOnly();
                return;
            }

            if (dayEndTransition == null || dragStartDay < 1)
            {
                GoNextDayNow();
                return;
            }

            waitingForTransition = true;
            dayEndTransition.Play(dragStartDay, GoNextDayAfterTransition);
        }

        private void PlayTransitionOnly()
        {
            if (dayEndTransition == null || dragStartDay < 1)
                return;

            dayEndTransition.Play(dragStartDay);
        }

        private void GoNextDayAfterTransition()
        {
            waitingForTransition = false;

            if (!HasDayChangedSinceDragStarted())
                GoNextDayNow();
        }

        private void GoNextDayNow()
        {
            if (GameManager.Instance == null)
                return;

            GameManager.Instance.NextDay();
        }

        public void ResetSliderImmediate()
        {
            if (slider == null)
                return;

            slider.SetValueWithoutNotify(0f);
        }

        private void StartReturnToLeft()
        {
            StopReturnRoutine();
            returnRoutine = StartCoroutine(ReturnToLeft());
        }

        private IEnumerator ReturnToLeft()
        {
            if (slider == null)
                yield break;

            isReturning = true;
            float startValue = slider.value;
            float elapsed = 0f;

            while (elapsed < returnDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / returnDuration);
                t = 1f - Mathf.Pow(1f - t, 3f);
                slider.SetValueWithoutNotify(Mathf.Lerp(startValue, 0f, t));
                yield return null;
            }

            slider.SetValueWithoutNotify(0f);
            isReturning = false;
            returnRoutine = null;
        }

        private void StopReturnRoutine()
        {
            if (returnRoutine == null)
                return;

            StopCoroutine(returnRoutine);
            returnRoutine = null;
            isReturning = false;
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (clip == null)
                return;

            if (audioSource != null && audioSource.enabled && audioSource.gameObject.activeInHierarchy)
            {
                audioSource.PlayOneShot(clip);
                return;
            }

            PlayTemporaryUiSound(clip);
        }

        private void PlayTemporaryUiSound(AudioClip clip)
        {
            GameObject soundObject = new GameObject("NextDaySliderOneShotAudio");
            AudioSource tempSource = soundObject.AddComponent<AudioSource>();
            tempSource.playOnAwake = false;
            tempSource.loop = false;
            tempSource.spatialBlend = 0f;
            tempSource.clip = clip;
            tempSource.Play();
            Destroy(soundObject, clip.length + 0.1f);
        }
    }
}
