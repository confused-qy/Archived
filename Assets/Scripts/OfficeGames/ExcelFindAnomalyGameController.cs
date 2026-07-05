using System;
using System.Collections;
using System.Collections.Generic;
using EmployeeHandbook.ClueSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DailyGameManager = EmployeeHandbook.DailyTasks.GameManager;

namespace EmployeeHandbook.OfficeGames
{
    public class ExcelFindAnomalyGameController : MonoBehaviour
    {
        private const int RowCount = 4;
        private const int ColumnCount = 4;

        [Header("Data")]
        [SerializeField] private string tasksResourcePath = "excel_find_anomaly_tasks";
        [SerializeField] private int fallbackDayForTesting = 1;
        [SerializeField] private ClueNotebookClueList clueList;

        [Header("Document")]
        [SerializeField] private Text documentTitleText;
        [SerializeField] private TMP_Text documentTitleTmpText;
        [SerializeField] private Text ruleText;
        [SerializeField] private TMP_Text ruleTmpText;

        [Header("Table")]
        [SerializeField] private Transform tableRoot;
        [SerializeField] private ExcelAnomalyCellView[] cells = new ExcelAnomalyCellView[RowCount * ColumnCount];

        [Header("Buttons")]
        [SerializeField] private Button submitButton;

        [Header("Result")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private Text resultText;
        [SerializeField] private TMP_Text resultTmpText;
        [SerializeField] private string successMessage = "提交成功！";
        [SerializeField] private string failureMessage = "选择不正确，请重新检查。";
        [SerializeField] private float resultFadeInDuration = 0.18f;
        [SerializeField] private float resultVisibleDuration = 1.1f;
        [SerializeField] private float resultFadeOutDuration = 0.25f;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip cellClickClip;
        [SerializeField] private AudioClip submitSuccessClip;
        [SerializeField] private AudioClip submitFailureClip;

        private ExcelFindAnomalyTaskData[] tasks = Array.Empty<ExcelFindAnomalyTaskData>();
        private ExcelFindAnomalyTaskData currentTask;
        private readonly HashSet<string> selectedCellIds = new HashSet<string>();
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
            CacheCells();
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
            selectedCellIds.Clear();

            currentTask = FindTaskForDay(currentDay);

            if (currentTask == null)
            {
                SetDocumentTitle("暂无表格");
                SetRule("今天没有 Excel 异常审核任务。");
                ClearCells();
                SetSubmitInteractable(false);
                Debug.LogWarning("ExcelFindAnomalyGameController 没有找到第 " + currentDay + " 天的 ExcelFill 配置。", this);
                return;
            }

            SetDocumentTitle(currentTask.title);
            SetRule(currentTask.rule);
            SetupCells(currentTask);
            SetSubmitInteractable(true);
        }

        public void Submit()
        {
            if (currentTask == null)
                return;

            if (!AreSelectedCellsCorrect())
            {
                ShowResult(failureMessage);
                PlayOneShot(submitFailureClip);
                return;
            }

            currentTaskCompleted = true;
            completedTaskId = currentTask.taskId;
            SetSubmitInteractable(false);
            SetCellsInteractable(false);
            ShowResult(successMessage);
            if (!WillUnlockSuccessClue())
                PlayOneShot(submitSuccessClip);
            ReportCurrentTaskSuccess();
        }

        private void HandleCellClicked(string cellId)
        {
            if (currentTask == null || currentTaskCompleted || string.IsNullOrWhiteSpace(cellId))
                return;

            if (selectedCellIds.Contains(cellId))
                selectedCellIds.Remove(cellId);
            else
                selectedCellIds.Add(cellId);

            UpdateCellSelections();
            PlayOneShot(cellClickClip);
        }

        private bool AreSelectedCellsCorrect()
        {
            HashSet<string> correctCells = GetCorrectCellSet();
            if (selectedCellIds.Count != correctCells.Count)
                return false;

            foreach (string cellId in selectedCellIds)
            {
                if (!correctCells.Contains(cellId))
                    return false;
            }

            return true;
        }

        private HashSet<string> GetCorrectCellSet()
        {
            HashSet<string> result = new HashSet<string>();
            if (currentTask == null || currentTask.correctCells == null)
                return result;

            for (int i = 0; i < currentTask.correctCells.Length; i++)
            {
                string cellId = NormalizeCellId(currentTask.correctCells[i]);
                if (!string.IsNullOrWhiteSpace(cellId))
                    result.Add(cellId);
            }

            return result;
        }

        private void SetupCells(ExcelFindAnomalyTaskData task)
        {
            CacheCells();

            for (int row = 0; row < RowCount; row++)
            {
                for (int column = 0; column < ColumnCount; column++)
                {
                    ExcelAnomalyCellView cell = FindCell(GetCellId(row, column));
                    if (cell == null)
                        continue;

                    string value = GetCellValue(task, row, column);
                    cell.SetText(value);
                    cell.SetSelected(false);
                    cell.SetInteractable(true);
                }
            }
        }

        private string GetCellValue(ExcelFindAnomalyTaskData task, int row, int column)
        {
            if (task == null || task.rows == null || row < 0 || row >= task.rows.Length)
                return string.Empty;

            ExcelFindAnomalyRowData rowData = task.rows[row];
            if (rowData == null || rowData.cells == null || column < 0 || column >= rowData.cells.Length)
                return string.Empty;

            return rowData.cells[column] ?? string.Empty;
        }

        private void ClearCells()
        {
            CacheCells();

            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] != null)
                    cells[i].Clear();
            }
        }

        private void UpdateCellSelections()
        {
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] != null)
                    cells[i].SetSelected(selectedCellIds.Contains(cells[i].CellId));
            }
        }

        private void SetCellsInteractable(bool interactable)
        {
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] != null)
                    cells[i].SetInteractable(interactable);
            }
        }

        private void CacheCells()
        {
            if (cells == null || cells.Length != RowCount * ColumnCount)
                cells = new ExcelAnomalyCellView[RowCount * ColumnCount];

            for (int row = 0; row < RowCount; row++)
            {
                for (int column = 0; column < ColumnCount; column++)
                {
                    string cellId = GetCellId(row, column);
                    int index = GetIndex(row, column);
                    ExcelAnomalyCellView cell = cells[index];

                    if (cell == null && tableRoot != null)
                    {
                        Transform child = tableRoot.Find(cellId);
                        if (child != null)
                        {
                            cell = child.GetComponent<ExcelAnomalyCellView>();
                            if (cell == null)
                                cell = child.gameObject.AddComponent<ExcelAnomalyCellView>();
                        }
                    }

                    if (cell != null)
                    {
                        cell.Initialize(cellId, HandleCellClicked);
                        cells[index] = cell;
                    }
                }
            }
        }

        private ExcelAnomalyCellView FindCell(string cellId)
        {
            cellId = NormalizeCellId(cellId);

            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] != null && cells[i].CellId == cellId)
                    return cells[i];
            }

            return null;
        }

        private void LoadTasks()
        {
            TextAsset textAsset = Resources.Load<TextAsset>(tasksResourcePath);
            if (textAsset == null)
            {
                tasks = Array.Empty<ExcelFindAnomalyTaskData>();
                Debug.LogWarning("没有找到 Resources/" + tasksResourcePath + ".json。", this);
                return;
            }

            ExcelFindAnomalyTaskCollection collection =
                JsonUtility.FromJson<ExcelFindAnomalyTaskCollection>(textAsset.text);
            tasks = collection != null && collection.tasks != null
                ? collection.tasks
                : Array.Empty<ExcelFindAnomalyTaskData>();
        }

        private ExcelFindAnomalyTaskData FindTaskForDay(int day)
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

        private void SetRule(string value)
        {
            if (ruleText != null)
                ruleText.text = value;

            if (ruleTmpText != null)
                ruleTmpText.text = value;
        }

        private void ShowResult(string message)
        {
            if (resultText != null)
                resultText.text = message;

            if (resultTmpText != null)
                resultTmpText.text = message;

            if (resultPanel == null && resultText == null && resultTmpText == null)
            {
                Debug.Log(message, this);
                return;
            }

            if (resultPanel == null)
                return;

            if (resultRoutine != null)
                StopCoroutine(resultRoutine);

            resultRoutine = StartCoroutine(PlayResultPopup());
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

        private IEnumerator PlayResultPopup()
        {
            if (resultPanel == null)
                yield break;

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
                Debug.LogWarning("ExcelFindAnomalyGameController 找不到 DailyTasks.GameManager，无法完成任务 " + currentTask.taskId + "。", this);
        }

        private void UnlockSuccessClues()
        {
            EnsureClueList();

            if (clueList == null || currentTask.unlockClueIdsOnSuccess == null)
                return;

            for (int i = 0; i < currentTask.unlockClueIdsOnSuccess.Length; i++)
            {
                int clueId = currentTask.unlockClueIdsOnSuccess[i];
                if (clueId > 0)
                    clueList.UnlockClue(clueId);
            }
        }

        private bool WillUnlockSuccessClue()
        {
            EnsureClueList();

            return clueList != null &&
                   currentTask != null &&
                   clueList.WillUnlockAnyClue(currentTask.unlockClueIdsOnSuccess);
        }

        private void EnsureClueList()
        {
            if (clueList != null)
                return;

            ClueNotebookClueList[] clueLists = Resources.FindObjectsOfTypeAll<ClueNotebookClueList>();
            for (int i = 0; i < clueLists.Length; i++)
            {
                if (clueLists[i] != null && clueLists[i].gameObject.scene.IsValid())
                {
                    clueList = clueLists[i];
                    return;
                }
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

        private static int GetIndex(int row, int column)
        {
            return row * ColumnCount + column;
        }

        private static string GetCellId(int row, int column)
        {
            return row.ToString() + column.ToString();
        }

        private static string NormalizeCellId(string cellId)
        {
            return string.IsNullOrWhiteSpace(cellId) ? string.Empty : cellId.Trim();
        }
    }

    [Serializable]
    public class ExcelFindAnomalyTaskCollection
    {
        public ExcelFindAnomalyTaskData[] tasks;
    }

    [Serializable]
    public class ExcelFindAnomalyTaskData
    {
        public int day;
        public string taskId;
        public string title;
        [TextArea(2, 6)] public string rule;
        public ExcelFindAnomalyRowData[] rows;
        public string[] correctCells;
        public int[] unlockClueIdsOnSuccess;
    }

    [Serializable]
    public class ExcelFindAnomalyRowData
    {
        public string[] cells;
    }
}
