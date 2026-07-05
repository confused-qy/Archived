using System;
using System.Collections;
using System.Collections.Generic;
using EmployeeHandbook.ClueSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DailyGameManager = EmployeeHandbook.DailyTasks.GameManager;

namespace EmployeeHandbook.NewsPopup
{
    public class NewsPopupController : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private string popupsResourcePath = "news_popups";
        [SerializeField] private float defaultPopupDelay = 10f;
        [SerializeField] private ClueNotebookClueList clueList;

        [Header("UI")]
        [SerializeField] private RectTransform popupPanel;
        [SerializeField] private Text headerText;
        [SerializeField] private TMP_Text headerTmpText;
        [SerializeField] private Text titleText;
        [SerializeField] private TMP_Text titleTmpText;
        [SerializeField] private Text bodyText;
        [SerializeField] private TMP_Text bodyTmpText;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button viewButton;
        [SerializeField] private Button ignoreButton;

        [Header("Detail Panel")]
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private Text detailBodyText;
        [SerializeField] private TMP_Text detailBodyTmpText;
        [SerializeField] private Button detailCloseButton;

        [Header("Animation")]
        [SerializeField] private float slideDistance = 180f;
        [SerializeField] private float showDuration = 0.28f;
        [SerializeField] private float hideDuration = 0.22f;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip popupSound;
        [SerializeField] private AudioClip closeSound;

        private NewsPopupData[] popups = Array.Empty<NewsPopupData>();
        private readonly HashSet<string> shownPopupIds = new HashSet<string>();
        private NewsPopupData currentPopup;
        private CanvasGroup canvasGroup;
        private CanvasGroup detailCanvasGroup;
        private Coroutine delayRoutine;
        private Coroutine animationRoutine;
        private bool subscribedToGameManager;
        private Vector2 shownAnchoredPosition;
        private int scheduledDay = -1;
        private bool currentPopupViewed;

        private void Awake()
        {
            AutoFindReferences();
            LoadPopups();
            BindButtons();
            SetupAudioSource();
            CachePanelPosition();
            HideImmediately();
        }

        private void OnEnable()
        {
            SubscribeToGameManager();
            RefreshForCurrentDay();
        }

        private void Start()
        {
            SubscribeToGameManager();
            RefreshForCurrentDay();
        }

        private void OnDisable()
        {
            UnsubscribeFromGameManager();
            StopDelay();
            StopAnimation();
        }

        public void RefreshForCurrentDay()
        {
            LoadPopups();

            int day = GetCurrentDay();
            NewsPopupData popup = FindPopupForDay(day);
            if (popup == null || WasShown(popup))
            {
                StopDelay();
                return;
            }

            if (delayRoutine != null && scheduledDay == day)
                return;

            StopDelay();
            scheduledDay = day;
            float delay = popup.delaySeconds > 0f ? popup.delaySeconds : defaultPopupDelay;
            Debug.Log("NewsPopupController 已安排第 " + day + " 天弹窗：" + GetPopupKey(popup) + "，延迟 " + delay + " 秒。", this);
            delayRoutine = StartCoroutine(ShowAfterDelay(popup, delay));
        }

        public void ClosePopup()
        {
            if (currentPopup == null)
            {
                HideImmediately();
                return;
            }

            PlayOneShot(closeSound);

            if (animationRoutine != null)
                StopCoroutine(animationRoutine);

            animationRoutine = StartCoroutine(HidePopup());
        }

        public void ViewPopupDetail()
        {
            if (currentPopup == null)
                return;

            SetDetailBody(currentPopup.body);
            ShowDetailPanel();

            if (!currentPopupViewed)
            {
                currentPopupViewed = true;
                UnlockViewClue(currentPopup);
            }
        }

        private IEnumerator ShowAfterDelay(NewsPopupData popup, float delay)
        {
            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);

            delayRoutine = null;

            if (popup == null || WasShown(popup) || GetCurrentDay() != popup.day)
                yield break;

            ShowPopup(popup);
        }

        private void ShowPopup(NewsPopupData popup)
        {
            currentPopup = popup;
            currentPopupViewed = false;
            shownPopupIds.Add(GetPopupKey(popup));
            SetHeader(popup.tag);
            SetTitle(popup.title);
            SetBody(string.Empty);
            SetDetailBody(popup.body);
            HideDetailPanel();
            PlayOneShot(popupSound);
            Debug.Log("NewsPopupController 显示弹窗：" + GetPopupKey(popup), this);

            if (animationRoutine != null)
                StopCoroutine(animationRoutine);

            animationRoutine = StartCoroutine(ShowPopupAnimation());
        }

        private IEnumerator ShowPopupAnimation()
        {
            if (popupPanel == null)
                yield break;

            if (!popupPanel.gameObject.activeSelf)
                popupPanel.gameObject.SetActive(true);
            EnsureCanvasGroup();

            Vector2 hiddenPosition = shownAnchoredPosition + new Vector2(0f, -slideDistance);
            popupPanel.anchoredPosition = hiddenPosition;
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            yield return AnimatePopup(hiddenPosition, shownAnchoredPosition, 0f, 1f, showDuration);

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            animationRoutine = null;
        }

        private IEnumerator HidePopup()
        {
            if (popupPanel == null)
                yield break;

            EnsureCanvasGroup();
            Vector2 hiddenPosition = shownAnchoredPosition + new Vector2(0f, -slideDistance);
            yield return AnimatePopup(popupPanel.anchoredPosition, hiddenPosition, canvasGroup.alpha, 0f, hideDuration);

            HideImmediately();
            HideDetailPanel();
            currentPopup = null;
            currentPopupViewed = false;
            animationRoutine = null;
        }

        private IEnumerator AnimatePopup(Vector2 fromPosition, Vector2 toPosition, float fromAlpha, float toAlpha, float duration)
        {
            if (duration <= 0f)
            {
                ApplyPopupFrame(toPosition, toAlpha);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = 1f - Mathf.Pow(1f - t, 3f);
                ApplyPopupFrame(Vector2.Lerp(fromPosition, toPosition, t), Mathf.Lerp(fromAlpha, toAlpha, t));
                yield return null;
            }

            ApplyPopupFrame(toPosition, toAlpha);
        }

        private void ApplyPopupFrame(Vector2 position, float alpha)
        {
            if (popupPanel != null)
                popupPanel.anchoredPosition = position;

            if (canvasGroup != null)
                canvasGroup.alpha = alpha;
        }

        private void HideImmediately()
        {
            if (popupPanel == null)
                return;

            EnsureCanvasGroup();
            popupPanel.anchoredPosition = shownAnchoredPosition + new Vector2(0f, -slideDistance);
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            HideDetailPanel();
        }

        private void UnlockViewClue(NewsPopupData popup)
        {
            if (popup == null || clueList == null || popup.clueIdOnClose <= 0)
                return;

            clueList.UnlockClue(popup.clueIdOnClose);
        }

        private void LoadPopups()
        {
            TextAsset textAsset = Resources.Load<TextAsset>(popupsResourcePath);
            if (textAsset == null)
            {
                popups = Array.Empty<NewsPopupData>();
                Debug.LogWarning("没有找到 Resources/" + popupsResourcePath + ".json。", this);
                return;
            }

            NewsPopupCollection collection = JsonUtility.FromJson<NewsPopupCollection>(textAsset.text);
            popups = collection != null && collection.popups != null ? collection.popups : Array.Empty<NewsPopupData>();
        }

        private NewsPopupData FindPopupForDay(int day)
        {
            for (int i = 0; i < popups.Length; i++)
            {
                if (popups[i] != null && popups[i].day == day)
                    return popups[i];
            }

            return null;
        }

        private bool WasShown(NewsPopupData popup)
        {
            return popup != null && shownPopupIds.Contains(GetPopupKey(popup));
        }

        private string GetPopupKey(NewsPopupData popup)
        {
            if (popup == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(popup.popupId))
                return popup.popupId;

            return "day_" + popup.day;
        }

        private int GetCurrentDay()
        {
            if (DailyGameManager.Instance != null && DailyGameManager.Instance.CurrentState != null)
                return DailyGameManager.Instance.CurrentState.currentDay;

            return 1;
        }

        private void SetHeader(string value)
        {
            if (headerText != null)
                headerText.text = value;

            if (headerTmpText != null)
                headerTmpText.text = value;
        }

        private void SetTitle(string value)
        {
            if (titleText != null)
                titleText.text = value;

            if (titleTmpText != null)
                titleTmpText.text = value;
        }

        private void SetBody(string value)
        {
            if (bodyText != null)
                bodyText.text = value;

            if (bodyTmpText != null)
                bodyTmpText.text = value;
        }

        private void SetDetailBody(string value)
        {
            if (detailBodyText != null)
                detailBodyText.text = value;

            if (detailBodyTmpText != null)
                detailBodyTmpText.text = value;
        }

        private void ShowDetailPanel()
        {
            if (detailPanel == null)
                return;

            if (!detailPanel.activeSelf)
                detailPanel.SetActive(true);

            EnsureDetailCanvasGroup();
            if (detailCanvasGroup != null)
            {
                detailCanvasGroup.alpha = 1f;
                detailCanvasGroup.interactable = true;
                detailCanvasGroup.blocksRaycasts = true;
            }
        }

        private void HideDetailPanel()
        {
            if (detailPanel == null)
                return;

            EnsureDetailCanvasGroup();
            if (detailCanvasGroup != null)
            {
                detailCanvasGroup.alpha = 0f;
                detailCanvasGroup.interactable = false;
                detailCanvasGroup.blocksRaycasts = false;
            }

            detailPanel.SetActive(false);
        }

        private void BindButtons()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(ClosePopup);
                closeButton.onClick.AddListener(ClosePopup);
            }

            if (ignoreButton != null)
            {
                ignoreButton.onClick.RemoveListener(ClosePopup);
                ignoreButton.onClick.AddListener(ClosePopup);
            }

            if (viewButton != null)
            {
                viewButton.onClick.RemoveListener(ViewPopupDetail);
                viewButton.onClick.AddListener(ViewPopupDetail);
            }

            if (detailCloseButton != null)
            {
                detailCloseButton.onClick.RemoveListener(ClosePopup);
                detailCloseButton.onClick.AddListener(ClosePopup);
            }
        }

        private void SetupAudioSource()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.playOnAwake = false;
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (clip == null || audioSource == null)
                return;

            if (!audioSource.enabled)
                audioSource.enabled = true;

            audioSource.PlayOneShot(clip);
        }

        private void AutoFindReferences()
        {
            if (popupPanel == null)
                popupPanel = transform as RectTransform;

            if (headerTmpText == null)
                headerTmpText = FindTmpText("Header");

            if (headerText == null)
                headerText = FindText("Header");

            if (titleTmpText == null)
                titleTmpText = FindTmpText("标题");

            if (titleText == null)
                titleText = FindText("标题");

            if (bodyTmpText == null)
                bodyTmpText = FindTmpText("内容");

            if (bodyText == null)
                bodyText = FindText("内容");

            if (closeButton == null)
            {
                Transform close = transform.Find("CloseButton");
                if (close != null)
                    closeButton = close.GetComponent<Button>();
            }

            if (viewButton == null)
                viewButton = FindButton("查看");

            if (ignoreButton == null)
                ignoreButton = FindButton("忽略");

            if (detailPanel == null)
            {
                Transform detail = transform.Find("DetailPanel");
                if (detail == null)
                    detail = transform.Find("详情");
                if (detail == null)
                    detail = transform.Find("内容详情");

                if (detail != null)
                    detailPanel = detail.gameObject;
            }

            if (detailPanel != null)
            {
                if (detailBodyTmpText == null)
                    detailBodyTmpText = FindTmpTextIn(detailPanel.transform, "内容");

                if (detailBodyText == null)
                    detailBodyText = FindTextIn(detailPanel.transform, "内容");

                if (detailCloseButton == null)
                {
                    Transform close = detailPanel.transform.Find("CloseButton");
                    if (close != null)
                        detailCloseButton = close.GetComponent<Button>();
                }
            }
        }

        private Button FindButton(string childName)
        {
            Transform child = transform.Find(childName);
            return child != null ? child.GetComponentInChildren<Button>(true) : null;
        }

        private TMP_Text FindTmpText(string childName)
        {
            Transform child = transform.Find(childName);
            return child != null ? child.GetComponentInChildren<TMP_Text>(true) : null;
        }

        private Text FindText(string childName)
        {
            Transform child = transform.Find(childName);
            return child != null ? child.GetComponentInChildren<Text>(true) : null;
        }

        private TMP_Text FindTmpTextIn(Transform root, string childName)
        {
            if (root == null)
                return null;

            Transform child = root.Find(childName);
            return child != null ? child.GetComponentInChildren<TMP_Text>(true) : null;
        }

        private Text FindTextIn(Transform root, string childName)
        {
            if (root == null)
                return null;

            Transform child = root.Find(childName);
            return child != null ? child.GetComponentInChildren<Text>(true) : null;
        }

        private void CachePanelPosition()
        {
            if (popupPanel == null)
                return;

            shownAnchoredPosition = popupPanel.anchoredPosition;
            EnsureCanvasGroup();
        }

        private void EnsureCanvasGroup()
        {
            if (popupPanel == null)
                return;

            canvasGroup = popupPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = popupPanel.gameObject.AddComponent<CanvasGroup>();
        }

        private void EnsureDetailCanvasGroup()
        {
            if (detailPanel == null)
                return;

            detailCanvasGroup = detailPanel.GetComponent<CanvasGroup>();
            if (detailCanvasGroup == null)
                detailCanvasGroup = detailPanel.AddComponent<CanvasGroup>();
        }

        private void StopDelay()
        {
            if (delayRoutine == null)
                return;

            StopCoroutine(delayRoutine);
            delayRoutine = null;
        }

        private void StopAnimation()
        {
            if (animationRoutine == null)
                return;

            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        private void SubscribeToGameManager()
        {
            if (subscribedToGameManager || DailyGameManager.Instance == null)
                return;

            DailyGameManager.Instance.StateChanged += RefreshForCurrentDay;
            subscribedToGameManager = true;
        }

        private void UnsubscribeFromGameManager()
        {
            if (!subscribedToGameManager || DailyGameManager.Instance == null)
                return;

            DailyGameManager.Instance.StateChanged -= RefreshForCurrentDay;
            subscribedToGameManager = false;
        }
    }

    [Serializable]
    public class NewsPopupCollection
    {
        public NewsPopupData[] popups;
    }

    [Serializable]
    public class NewsPopupData
    {
        public int day;
        public string popupId;
        public float delaySeconds;
        public string tag;
        public string title;
        [TextArea(2, 6)] public string body;
        public int clueIdOnClose;
    }
}
