using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace EmployeeHandbook.DailyTasks
{
    /// <summary>
    /// 根据当天已完成任务数量切换镜子上的状态图片。
    /// 0 个任务使用第 0 张，1 个任务使用第 1 张，以此类推；超过图片数量时使用最后一张。
    /// </summary>
    public class MirrorTaskStateImage : MonoBehaviour
    {
        [Header("Image")]
        [FormerlySerializedAs("mirrorImage")]
        [SerializeField] private Image currentImage;

        [Header("Sprites")]
        [SerializeField] private Sprite[] stateSprites = new Sprite[4];
        [SerializeField] private bool preserveNativeSize;

        [Header("Transition")]
        [SerializeField] private float transitionDuration = 0.35f;

        private bool subscribed;
        private int currentSpriteIndex = -1;
        private Coroutine transitionRoutine;
        private Image transitionImage;

        private void Awake()
        {
            if (currentImage == null)
                currentImage = GetComponent<Image>();

            SetupImageAlpha(currentImage, 1f);
            EnsureTransitionImage();
            SetupImageAlpha(transitionImage, 0f);
        }

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void Start()
        {
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Refresh()
        {
            if (currentImage == null || stateSprites == null || stateSprites.Length == 0)
                return;

            int completedCount = GetTodayCompletedTaskCount();
            int spriteIndex = Mathf.Clamp(completedCount, 0, stateSprites.Length - 1);
            ApplySpriteIndex(spriteIndex, currentSpriteIndex < 0);
        }

        private void ApplySpriteIndex(int spriteIndex, bool immediate)
        {
            if (spriteIndex == currentSpriteIndex)
                return;

            Sprite targetSprite = stateSprites[spriteIndex];

            if (targetSprite == null)
                return;

            if (transitionRoutine != null)
                StopCoroutine(transitionRoutine);

            EnsureTransitionImage();

            if (immediate || transitionImage == null || transitionDuration <= 0f)
            {
                currentImage.sprite = targetSprite;
                SetupImageAlpha(currentImage, 1f);
                SetupImageAlpha(transitionImage, 0f);
                currentSpriteIndex = spriteIndex;

                if (preserveNativeSize)
                    currentImage.SetNativeSize();

                return;
            }

            transitionImage.sprite = targetSprite;
            SetupImageAlpha(currentImage, 1f);
            SetupImageAlpha(transitionImage, 0f);

            if (preserveNativeSize)
                transitionImage.SetNativeSize();

            transitionRoutine = StartCoroutine(CrossFadeTo(spriteIndex));
        }

        private IEnumerator CrossFadeTo(int spriteIndex)
        {
            float elapsed = 0f;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);
                t = 1f - Mathf.Pow(1f - t, 3f);

                SetupImageAlpha(currentImage, 1f - t);
                SetupImageAlpha(transitionImage, t);
                yield return null;
            }

            currentImage.sprite = transitionImage.sprite;
            SetupImageAlpha(currentImage, 1f);
            SetupImageAlpha(transitionImage, 0f);
            currentSpriteIndex = spriteIndex;
            transitionRoutine = null;

            if (preserveNativeSize)
                currentImage.SetNativeSize();
        }

        private void EnsureTransitionImage()
        {
            if (transitionImage != null || currentImage == null)
                return;

            GameObject transitionObject = new GameObject("MirrorTransitionImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            transitionObject.transform.SetParent(currentImage.transform.parent, false);
            transitionObject.transform.SetSiblingIndex(currentImage.transform.GetSiblingIndex() + 1);

            RectTransform sourceRect = currentImage.rectTransform;
            RectTransform transitionRect = transitionObject.GetComponent<RectTransform>();
            transitionRect.anchorMin = sourceRect.anchorMin;
            transitionRect.anchorMax = sourceRect.anchorMax;
            transitionRect.pivot = sourceRect.pivot;
            transitionRect.anchoredPosition = sourceRect.anchoredPosition;
            transitionRect.sizeDelta = sourceRect.sizeDelta;
            transitionRect.localRotation = sourceRect.localRotation;
            transitionRect.localScale = sourceRect.localScale;

            transitionImage = transitionObject.GetComponent<Image>();
            transitionImage.raycastTarget = false;
            transitionImage.preserveAspect = currentImage.preserveAspect;
            transitionImage.type = currentImage.type;
            transitionImage.material = currentImage.material;
            transitionImage.color = currentImage.color;
            SetupImageAlpha(transitionImage, 0f);
        }

        private void SetupImageAlpha(Image image, float alpha)
        {
            if (image == null)
                return;

            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }

        private int GetTodayCompletedTaskCount()
        {
            if (GameManager.Instance == null || GameManager.Instance.TaskManager == null)
                return 0;

            List<TaskData> todayTasks = GameManager.Instance.TaskManager.GetTodayTasks();
            int completedCount = 0;

            for (int i = 0; i < todayTasks.Count; i++)
            {
                TaskData task = todayTasks[i];
                if (task != null && task.completed)
                    completedCount++;
            }

            return completedCount;
        }

        private void Subscribe()
        {
            if (subscribed || GameManager.Instance == null)
                return;

            GameManager.Instance.StateChanged += Refresh;

            if (GameManager.Instance.TaskManager != null)
                GameManager.Instance.TaskManager.TasksChanged += Refresh;

            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || GameManager.Instance == null)
                return;

            GameManager.Instance.StateChanged -= Refresh;

            if (GameManager.Instance.TaskManager != null)
                GameManager.Instance.TaskManager.TasksChanged -= Refresh;

            subscribed = false;
        }
    }
}
