using System;
using System.Collections;
using EmployeeHandbook.ClueSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DailyGameManager = EmployeeHandbook.DailyTasks.GameManager;

namespace EmployeeHandbook.OfficeGames
{
    public class WordFillGameController : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private string tasksResourcePath = "word_fill_tasks";
        [SerializeField] private int fallbackDayForTesting = 1;
        [SerializeField] private ClueNotebookClueList clueList;

        [Header("Document")]
        [SerializeField] private Text documentTitleText;
        [SerializeField] private TMP_Text documentTitleTmpText;
        [SerializeField] private Text paragraphText;
        [SerializeField] private TMP_Text paragraphTmpText;

        [Header("Questions")]
        [SerializeField] private WordFillQuestionView[] questions = new WordFillQuestionView[2];

        [Header("Buttons")]
        [SerializeField] private Button submitButton;

        [Header("Result")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private Text resultText;
        [SerializeField] private TMP_Text resultTmpText;
        [SerializeField] private string successMessage = "提交成功！";
        [SerializeField] private string failureMessage = "答案不正确，请重新检查。";
        [SerializeField] private float resultFadeInDuration = 0.18f;
        [SerializeField] private float resultVisibleDuration = 1.1f;
        [SerializeField] private float resultFadeOutDuration = 0.25f;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip submitSuccessClip;
        [SerializeField] private AudioClip submitFailureClip;

        private WordFillTaskData[] tasks = Array.Empty<WordFillTaskData>();
        private WordFillTaskData currentTask;
        private bool subscribedToGameManager;
        private CanvasGroup resultCanvasGroup;
        private Coroutine resultRoutine;
        private bool currentTaskCompleted;
        private int loadedDay = -1;
        private string completedTaskId;

        private void Awake()
        {
            LoadTasks();
            SetupAudioSource();
            BindButtons();
            CacheResultPanel();
            HideResult();
        }

        private void OnEnable()
        {
            SubscribeToGameManager();
            RefreshForCurrentDay();
        }

        private void OnDisable()
        {
            UnsubscribeFromGameManager();
            HideResult();
        }

        public void RefreshForCurrentDay()
        {
            int currentDay = GetCurrentDay();

            if (currentTaskCompleted && loadedDay == currentDay && currentTask != null && currentTask.taskId == completedTaskId)
            {
                SetSubmitInteractable(false);
                return;
            }

            LoadTasks();
            HideResult();
            loadedDay = currentDay;
            currentTaskCompleted = false;
            completedTaskId = null;

            currentTask = FindTaskForDay(currentDay);

            if (currentTask == null)
            {
                SetDocumentTitle("暂无文档");
                SetParagraph("今天没有 Word 填写任务。");
                SetupQuestions(null);
                SetSubmitInteractable(false);
                Debug.LogWarning("WordFillGameController 没有找到第 " + currentDay + " 天的 WordFill 配置。", this);
                return;
            }

            SetDocumentTitle(currentTask.title);
            SetParagraph(currentTask.paragraph);
            SetupQuestions(currentTask.questions);
            SetSubmitInteractable(true);
        }

        public void Submit()
        {
            if (currentTask == null)
                return;

            if (!AreAllAnswersCorrect())
            {
                ShowResult(failureMessage);
                PlayOneShot(submitFailureClip);
                return;
            }

            currentTaskCompleted = true;
            completedTaskId = currentTask.taskId;
            SetSubmitInteractable(false);
            ShowResult(successMessage);
            PlayOneShot(submitSuccessClip);
            ReportCurrentTaskSuccess();
        }

        private bool AreAllAnswersCorrect()
        {
            int requiredCount = currentTask.questions == null ? 0 : currentTask.questions.Length;
            if (requiredCount == 0)
                return false;

            for (int i = 0; i < requiredCount; i++)
            {
                if (i >= questions.Length || questions[i] == null || !questions[i].IsCorrect())
                    return false;
            }

            return true;
        }

        private void SetupQuestions(WordFillQuestionData[] questionData)
        {
            for (int i = 0; i < questions.Length; i++)
            {
                if (questions[i] == null)
                    continue;

                WordFillQuestionData question = questionData != null && i < questionData.Length ? questionData[i] : null;
                questions[i].Setup(question);
                questions[i].SetInteractable(question != null);
            }
        }

        private void LoadTasks()
        {
            TextAsset textAsset = Resources.Load<TextAsset>(tasksResourcePath);
            if (textAsset == null)
            {
                tasks = Array.Empty<WordFillTaskData>();
                Debug.LogWarning("没有找到 Resources/" + tasksResourcePath + ".json。", this);
                return;
            }

            WordFillTaskCollection collection = JsonUtility.FromJson<WordFillTaskCollection>(textAsset.text);
            tasks = collection != null && collection.tasks != null ? collection.tasks : Array.Empty<WordFillTaskData>();
        }

        private WordFillTaskData FindTaskForDay(int day)
        {
            for (int i = 0; i < tasks.Length; i++)
            {
                if (tasks[i] != null && tasks[i].day == day)
                    return tasks[i];
            }

            return null;
        }

        private int GetCurrentDay()
        {
            if (DailyGameManager.Instance != null && DailyGameManager.Instance.CurrentState != null)
                return DailyGameManager.Instance.CurrentState.currentDay;

            return Mathf.Max(1, fallbackDayForTesting);
        }

        private void SetDocumentTitle(string value)
        {
            if (documentTitleText != null)
                documentTitleText.text = value;

            if (documentTitleTmpText != null)
                documentTitleTmpText.text = value;
        }

        private void SetParagraph(string value)
        {
            if (paragraphText != null)
                paragraphText.text = value;

            if (paragraphTmpText != null)
                paragraphTmpText.text = value;
        }

        private void ShowResult(string message, Action afterHidden = null)
        {
            if (resultText != null)
                resultText.text = message;

            if (resultTmpText != null)
                resultTmpText.text = message;

            if (resultPanel == null && resultText == null && resultTmpText == null)
            {
                Debug.Log(message, this);
                afterHidden?.Invoke();
                return;
            }

            if (resultPanel == null)
            {
                afterHidden?.Invoke();
                return;
            }

            if (resultRoutine != null)
                StopCoroutine(resultRoutine);

            resultRoutine = StartCoroutine(PlayResultPopup(afterHidden));
        }

        private void HideResult()
        {
            if (resultRoutine != null)
            {
                StopCoroutine(resultRoutine);
                resultRoutine = null;
            }

            if (resultCanvasGroup == null)
                CacheResultPanel();

            if (resultCanvasGroup != null)
            {
                resultCanvasGroup.alpha = 0f;
                resultCanvasGroup.interactable = false;
                resultCanvasGroup.blocksRaycasts = false;
            }

            if (resultPanel != null)
                resultPanel.SetActive(false);

            if (resultText != null)
                resultText.text = string.Empty;

            if (resultTmpText != null)
                resultTmpText.text = string.Empty;
        }

        private void CacheResultPanel()
        {
            if (resultPanel == null)
                return;

            resultCanvasGroup = resultPanel.GetComponent<CanvasGroup>();
            if (resultCanvasGroup == null)
                resultCanvasGroup = resultPanel.AddComponent<CanvasGroup>();

            resultCanvasGroup.interactable = false;
            resultCanvasGroup.blocksRaycasts = false;
        }

        private IEnumerator PlayResultPopup(Action afterHidden)
        {
            if (resultPanel == null)
            {
                afterHidden?.Invoke();
                yield break;
            }

            if (resultCanvasGroup == null)
                CacheResultPanel();

            resultPanel.SetActive(true);

            if (resultCanvasGroup != null)
            {
                resultCanvasGroup.alpha = 0f;
                resultCanvasGroup.interactable = false;
                resultCanvasGroup.blocksRaycasts = false;
            }

            yield return FadeResult(0f, 1f, resultFadeInDuration);

            if (resultVisibleDuration > 0f)
                yield return new WaitForSecondsRealtime(resultVisibleDuration);

            yield return FadeResult(1f, 0f, resultFadeOutDuration);

            resultPanel.SetActive(false);
            resultRoutine = null;
            afterHidden?.Invoke();
        }

        private IEnumerator FadeResult(float fromAlpha, float toAlpha, float duration)
        {
            if (resultCanvasGroup == null)
                yield break;

            if (duration <= 0f)
            {
                resultCanvasGroup.alpha = toAlpha;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = 1f - Mathf.Pow(1f - t, 3f);
                resultCanvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
                yield return null;
            }

            resultCanvasGroup.alpha = toAlpha;
        }

        private void ReportCurrentTaskSuccess()
        {
            if (currentTask == null)
                return;

            UnlockSuccessClues();

            if (DailyGameManager.Instance != null)
                DailyGameManager.Instance.ReportMiniGameSuccess(currentTask.taskId);
            else
                Debug.LogWarning("WordFillGameController 找不到 DailyTasks.GameManager，无法完成任务 " + currentTask.taskId + "。", this);
        }

        private void UnlockSuccessClues()
        {
            if (clueList == null || currentTask.unlockClueIdsOnSuccess == null)
                return;

            for (int i = 0; i < currentTask.unlockClueIdsOnSuccess.Length; i++)
            {
                int clueId = currentTask.unlockClueIdsOnSuccess[i];
                if (clueId > 0)
                    clueList.UnlockClue(clueId);
            }
        }

        private void SetSubmitInteractable(bool interactable)
        {
            if (submitButton != null)
                submitButton.interactable = interactable;
        }

        private void BindButtons()
        {
            if (submitButton != null)
            {
                submitButton.onClick.RemoveListener(Submit);
                submitButton.onClick.AddListener(Submit);
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
    public class WordFillTaskCollection
    {
        public WordFillTaskData[] tasks;
    }

    [Serializable]
    public class WordFillTaskData
    {
        public int day;
        public string taskId;
        public string title;
        [TextArea(4, 12)] public string paragraph;
        public int[] unlockClueIdsOnSuccess;
        public WordFillQuestionData[] questions;
    }

    [Serializable]
    public class WordFillQuestionData
    {
        public string prompt;
        public string[] answers;
    }
}
