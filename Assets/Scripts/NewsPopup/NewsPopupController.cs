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
        private Coroutine delayRoutine;
        private Coroutine animationRoutine;
        private bool subscribedToGameManager;
        private Vector2 shownAnchoredPosition;
        private int scheduledDay = -1;

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

            UnlockCloseClue(currentPopup);
            PlayOneShot(closeSound);

            if (animationRoutine != null)
                StopCoroutine(animationRoutine);

            animationRoutine = StartCoroutine(HidePopup());
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
            shownPopupIds.Add(GetPopupKey(popup));
            SetHeader(popup.tag);
            SetTitle(popup.title);
            SetBody(popup.body);
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
            currentPopup = null;
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
        }

        private void UnlockCloseClue(NewsPopupData popup)
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

        private void BindButtons()
        {
            if (closeButton == null)
                return;

            closeButton.onClick.RemoveListener(ClosePopup);
            closeButton.onClick.AddListener(ClosePopup);
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
