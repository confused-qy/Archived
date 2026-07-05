using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EmployeeHandbook.DailyTasks
{
    /// <summary>
    /// 全局 Scene 淡入淡出转场。会自动创建黑色全屏遮罩并跨 Scene 保留。
    /// </summary>
    public class SceneTransitionController : MonoBehaviour
    {
        private const int OverlaySortingOrder = 32767;

        [SerializeField] private float fadeOutDuration = 0.35f;
        [SerializeField] private float fadeInDuration = 0.35f;
        [SerializeField] private float postLoadBlackHoldDuration = 0.1f;
        [SerializeField] private Color overlayColor = Color.black;

        private static SceneTransitionController instance;

        private CanvasGroup canvasGroup;
        private Image overlayImage;
        private bool isTransitioning;

        public static SceneTransitionController Instance
        {
            get
            {
                if (instance == null)
                    CreateRuntimeInstance();

                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureOverlay();
            SetAlpha(0f);
        }

        public static void LoadScene(string sceneName)
        {
            Instance.LoadSceneWithFade(sceneName);
        }

        public static void LoadSceneFromBlack(string sceneName)
        {
            Instance.LoadSceneFromBlackWithFade(sceneName);
        }

        public void LoadSceneWithFade(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning("SceneTransitionController 收到空 sceneName。", this);
                return;
            }

            if (isTransitioning)
                return;

            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        public void LoadSceneFromBlackWithFade(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning("SceneTransitionController 收到空 sceneName。", this);
                return;
            }

            if (isTransitioning)
                return;

            StartCoroutine(LoadSceneFromBlackRoutine(sceneName));
        }

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            isTransitioning = true;
            EnsureOverlay();

            yield return Fade(0f, 1f, fadeOutDuration);
            yield return SceneManager.LoadSceneAsync(sceneName);
            EnsureOverlay();
            SetAlpha(1f);
            yield return null;
            yield return null;

            if (postLoadBlackHoldDuration > 0f)
                yield return new WaitForSecondsRealtime(postLoadBlackHoldDuration);

            yield return Fade(1f, 0f, fadeInDuration);

            isTransitioning = false;
        }

        private IEnumerator LoadSceneFromBlackRoutine(string sceneName)
        {
            isTransitioning = true;
            EnsureOverlay();
            SetAlpha(1f);
            yield return null;

            yield return SceneManager.LoadSceneAsync(sceneName);
            EnsureOverlay();
            SetAlpha(1f);
            yield return null;
            yield return null;

            if (postLoadBlackHoldDuration > 0f)
                yield return new WaitForSecondsRealtime(postLoadBlackHoldDuration);

            yield return Fade(1f, 0f, fadeInDuration);

            isTransitioning = false;
        }

        private IEnumerator Fade(float fromAlpha, float toAlpha, float duration)
        {
            if (duration <= 0f)
            {
                SetAlpha(toAlpha);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = 1f - Mathf.Pow(1f - t, 3f);
                SetAlpha(Mathf.Lerp(fromAlpha, toAlpha, t));
                yield return null;
            }

            SetAlpha(toAlpha);
        }

        private void SetAlpha(float alpha)
        {
            EnsureOverlay();
            canvasGroup.alpha = alpha;
            canvasGroup.blocksRaycasts = alpha > 0.01f;
            canvasGroup.interactable = alpha > 0.01f;
        }

        private void EnsureOverlay()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
                canvas = gameObject.AddComponent<Canvas>();

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = OverlaySortingOrder;
            canvas.overrideSorting = true;

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = gameObject.AddComponent<CanvasScaler>();

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            Transform overlay = transform.Find("Overlay");
            if (overlay == null)
            {
                GameObject overlayObject = new GameObject("Overlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                overlayObject.transform.SetParent(transform, false);
                overlay = overlayObject.transform;
            }

            RectTransform rect = overlay as RectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            overlayImage = overlay.GetComponent<Image>();
            overlayImage.color = overlayColor;
            overlayImage.raycastTarget = true;
        }

        private static void CreateRuntimeInstance()
        {
            GameObject transitionObject = new GameObject("SceneTransitionController");
            instance = transitionObject.AddComponent<SceneTransitionController>();
        }
    }
}
