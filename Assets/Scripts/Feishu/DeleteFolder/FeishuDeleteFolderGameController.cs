using System;
using System.Collections;
using System.Collections.Generic;
using EmployeeHandbook.ClueSystem;
using EmployeeHandbook.DailyTasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmployeeHandbook.Feishu.DeleteFolder
{
    public class FeishuDeleteFolderGameController : MonoBehaviour
    {
        [SerializeField] private FeishuDeleteFileItem[] fileItems;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Sprite[] fileSprites;
        [SerializeField] private string fileConfigResourcePath = "delete_folder_files";
        [SerializeField] private TaskFileSet[] taskFileSets;
        [SerializeField] private ClueNotebookClueList clueList;
        [SerializeField] private GameObject resultPopup;
        [SerializeField] private Text resultText;
        [SerializeField] private TMP_Text resultTmpText;
        [SerializeField] private string successMessage = "删除成功！";
        [SerializeField] private string failureMessage = "删除失败！请重新删除";
        [SerializeField] private float popupFadeInDuration = 0.12f;
        [SerializeField] private float popupVisibleDuration = 0.85f;
        [SerializeField] private float popupFadeOutDuration = 0.2f;
        [SerializeField] private float popupStartScale = 0.9f;
        [SerializeField] private float popupOvershootScale = 1.06f;

        private readonly List<FeishuDeleteFileItem> selectedItems = new List<FeishuDeleteFileItem>();
        private TaskFileSet[] jsonTaskFileSets;
        private DayFileSet[] jsonDayFileSets;
        private TaskData todayDeleteTask;
        private TaskFileSet todayFileSet;
        private CanvasGroup resultPopupCanvasGroup;
        private Vector3 resultPopupBaseScale = Vector3.one;
        private Coroutine resultPopupRoutine;
        private bool canPlayToday;
        private bool dailyTaskEventsSubscribed;

        private void Awake()
        {
            LoadJsonFileSets();
            CacheResultPopup();

            if (deleteButton != null)
                deleteButton.onClick.AddListener(TryDeleteSelectedFiles);
        }

        private void OnEnable()
        {
            SubscribeToDailyTaskEvents();
            ReloadToday();
        }

        private void Start()
        {
            SubscribeToDailyTaskEvents();
            ReloadToday();
        }

        private void OnDisable()
        {
            UnsubscribeFromDailyTaskEvents();
            ClearSelection();
            HideResultPopupImmediately();
        }

        public void ReloadToday()
        {
            selectedItems.Clear();
            todayDeleteTask = FindTodayDeleteFolderTask();
            todayFileSet = todayDeleteTask != null ? FindFileSet(todayDeleteTask) : FindDayFileSet(GetCurrentDay());
            canPlayToday = todayDeleteTask != null && !todayDeleteTask.completed && todayFileSet != null;

            if (resultPopup != null)
                HideResultPopupImmediately();

            if (deleteButton != null)
                deleteButton.gameObject.SetActive(canPlayToday);

            RefreshFiles(todayDeleteTask != null && todayDeleteTask.completed);
        }

        private void SubscribeToDailyTaskEvents()
        {
            if (dailyTaskEventsSubscribed)
                return;

            EmployeeHandbook.DailyTasks.GameManager gameManager = EmployeeHandbook.DailyTasks.GameManager.Instance;
            if (gameManager == null)
                return;

            gameManager.StateChanged += HandleDailyTaskStateChanged;

            if (gameManager.TaskManager != null)
                gameManager.TaskManager.TasksChanged += HandleDailyTaskStateChanged;

            dailyTaskEventsSubscribed = true;
        }

        private void UnsubscribeFromDailyTaskEvents()
        {
            if (!dailyTaskEventsSubscribed)
                return;

            EmployeeHandbook.DailyTasks.GameManager gameManager = EmployeeHandbook.DailyTasks.GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.StateChanged -= HandleDailyTaskStateChanged;

                if (gameManager.TaskManager != null)
                    gameManager.TaskManager.TasksChanged -= HandleDailyTaskStateChanged;
            }

            dailyTaskEventsSubscribed = false;
        }

        private void HandleDailyTaskStateChanged()
        {
            if (!isActiveAndEnabled)
                return;

            ReloadToday();
        }

        public void TryDeleteSelectedFiles()
        {
            if (!canPlayToday || todayDeleteTask == null || todayFileSet == null)
                return;

            bool correct = IsSelectionCorrect();
            if (!correct)
            {
                ClearSelection();
                ShowResult(failureMessage);
                return;
            }

            for (int i = 0; i < fileItems.Length; i++)
            {
                if (fileItems[i] != null && fileItems[i].ShouldDelete)
                    fileItems[i].SetDeleted(true);
            }

            UnlockSuccessClues();

            if (EmployeeHandbook.DailyTasks.GameManager.Instance != null)
                EmployeeHandbook.DailyTasks.GameManager.Instance.ReportMiniGameSuccess(todayDeleteTask.taskId);
            else
                Debug.LogWarning("删除文件小游戏完成，但场景中没有 DailyTasks.GameManager。", this);

            canPlayToday = false;

            if (deleteButton != null)
                deleteButton.gameObject.SetActive(false);

            ShowResult(successMessage);
        }

        private TaskData FindTodayDeleteFolderTask()
        {
            if (EmployeeHandbook.DailyTasks.GameManager.Instance == null || EmployeeHandbook.DailyTasks.GameManager.Instance.TaskManager == null)
                return null;

            List<TaskData> tasks = EmployeeHandbook.DailyTasks.GameManager.Instance.TaskManager.GetTodayTasks();
            for (int i = 0; i < tasks.Count; i++)
            {
                TaskData task = tasks[i];
                if (task != null && task.taskType == TaskType.DeleteFolder)
                    return task;
            }

            return null;
        }

        private TaskFileSet FindFileSet(TaskData task)
        {
            if (task == null)
                return null;

            TaskFileSet inspectorSet = FindMatchingFileSet(taskFileSets, task.taskName);
            if (inspectorSet != null)
                return inspectorSet;

            TaskFileSet jsonSet = FindMatchingFileSet(jsonTaskFileSets, task.taskName);
            if (jsonSet != null)
                return jsonSet;

            return CreateDefaultFileSet(task);
        }

        private TaskFileSet FindMatchingFileSet(TaskFileSet[] sets, string taskName)
        {
            if (sets == null)
                return null;

            for (int i = 0; i < sets.Length; i++)
            {
                TaskFileSet set = sets[i];
                if (set != null && set.Matches(taskName))
                    return set;
            }

            return null;
        }

        private void LoadJsonFileSets()
        {
            jsonTaskFileSets = null;
            jsonDayFileSets = null;

            if (string.IsNullOrEmpty(fileConfigResourcePath))
                return;

            TextAsset jsonAsset = Resources.Load<TextAsset>(fileConfigResourcePath);
            if (jsonAsset == null)
            {
                Debug.LogWarning("删除文件小游戏找不到配置：Resources/" + fileConfigResourcePath + ".json，将使用脚本默认配置。", this);
                return;
            }

            TaskFileSetWrapper wrapper = JsonUtility.FromJson<TaskFileSetWrapper>(jsonAsset.text);
            if (wrapper == null || wrapper.taskFileSets == null)
            {
                Debug.LogWarning("删除文件小游戏配置解析失败：" + fileConfigResourcePath, this);
                return;
            }

            jsonTaskFileSets = wrapper.taskFileSets;
            jsonDayFileSets = wrapper.dayFileSets;
        }

        private TaskFileSet FindDayFileSet(int day)
        {
            if (jsonDayFileSets == null)
                return null;

            for (int i = 0; i < jsonDayFileSets.Length; i++)
            {
                DayFileSet set = jsonDayFileSets[i];
                if (set != null && set.day == day)
                    return TaskFileSet.Create("Day" + day, set.files);
            }

            return null;
        }

        private int GetCurrentDay()
        {
            if (EmployeeHandbook.DailyTasks.GameManager.Instance != null && EmployeeHandbook.DailyTasks.GameManager.Instance.CurrentState != null)
                return EmployeeHandbook.DailyTasks.GameManager.Instance.CurrentState.currentDay;

            return 1;
        }

        private TaskFileSet CreateDefaultFileSet(TaskData task)
        {
            if (task == null)
                return null;

            switch (task.taskName)
            {
                case "删除临时夹":
                    return TaskFileSet.Create(task.taskName, new[]
                    {
                        FileEntry.Create("d03_01", "今日截图缓存", true),
                        FileEntry.Create("d03_02", "测试导出包", true),
                        FileEntry.Create("d03_03", "临时压缩包", true),
                        FileEntry.Create("d03_04", "正式报表", false),
                        FileEntry.Create("d03_05", "入职资料", false),
                        FileEntry.Create("d03_06", "邮件备份", false)
                    });

                case "删除Word文件":
                    return TaskFileSet.Create(task.taskName, new[]
                    {
                        FileEntry.Create("d04_01", "旧版会议纪要", true),
                        FileEntry.Create("d04_02", "草稿说明文档", true),
                        FileEntry.Create("d04_03", "废弃通知", true),
                        FileEntry.Create("d04_04", "审核清单", false),
                        FileEntry.Create("d04_05", "发票汇总", false),
                        FileEntry.Create("d04_06", "员工名单", false)
                    });

                case "删除过期文件":
                    return TaskFileSet.Create(task.taskName, new[]
                    {
                        FileEntry.Create("d05_01", "过期申请表", true),
                        FileEntry.Create("d05_02", "上月临时表", true),
                        FileEntry.Create("d05_03", "旧版模板", true),
                        FileEntry.Create("d05_04", "今日任务表", false),
                        FileEntry.Create("d05_05", "合同扫描件", false),
                        FileEntry.Create("d05_06", "部门通讯录", false)
                    });

                case "删除过期夹":
                    return TaskFileSet.Create(task.taskName, new[]
                    {
                        FileEntry.Create("d06_01", "旧培训包", true),
                        FileEntry.Create("d06_02", "过期素材夹", true),
                        FileEntry.Create("d06_03", "临时下载夹", true),
                        FileEntry.Create("d06_04", "入职材料", false),
                        FileEntry.Create("d06_05", "项目附件", false),
                        FileEntry.Create("d06_06", "本周排班", false)
                    });

                case "清除空文件夹":
                    return TaskFileSet.Create(task.taskName, new[]
                    {
                        FileEntry.Create("d07_01", "空白归档", true),
                        FileEntry.Create("d07_02", "未命名文件夹", true),
                        FileEntry.Create("d07_03", "空附件包", true),
                        FileEntry.Create("d07_04", "报销凭证", false),
                        FileEntry.Create("d07_05", "会议纪要", false),
                        FileEntry.Create("d07_06", "工作交接", false)
                    });

                case "清理重复文件":
                    return TaskFileSet.Create(task.taskName, new[]
                    {
                        FileEntry.Create("d09_01", "报表副本", true),
                        FileEntry.Create("d09_02", "通知副本", true),
                        FileEntry.Create("d09_03", "名单副本", true),
                        FileEntry.Create("d09_04", "原始报表", false),
                        FileEntry.Create("d09_05", "正式通知", false),
                        FileEntry.Create("d09_06", "最新名单", false)
                    });

                case "删除会议文件":
                    return TaskFileSet.Create(task.taskName, new[]
                    {
                        FileEntry.Create("d13_01", "旧会议录音", true),
                        FileEntry.Create("d13_02", "临时会议图", true),
                        FileEntry.Create("d13_03", "废弃会议稿", true),
                        FileEntry.Create("d13_04", "正式会议纪要", false),
                        FileEntry.Create("d13_05", "参会名单", false),
                        FileEntry.Create("d13_06", "会议审批单", false)
                    });

                default:
                    return TaskFileSet.Create(task.taskName, new[]
                    {
                        FileEntry.Create("auto_01", task.taskName + "1", true),
                        FileEntry.Create("auto_02", task.taskName + "2", true),
                        FileEntry.Create("auto_03", task.taskName + "3", true),
                        FileEntry.Create("auto_04", "保留文件1", false),
                        FileEntry.Create("auto_05", "保留文件2", false),
                        FileEntry.Create("auto_06", "保留文件3", false)
                    });
            }
        }

        private void RefreshFiles(bool showDeleted)
        {
            for (int i = 0; i < fileItems.Length; i++)
            {
                FeishuDeleteFileItem item = fileItems[i];
                if (item == null)
                    continue;

                FileEntry entry = GetEntry(i);
                bool shouldDelete = entry != null && entry.shouldDelete;
                string fileName = entry != null && !string.IsNullOrEmpty(entry.fileName)
                    ? entry.fileName
                    : GetFallbackFileName(i);
                string fileId = entry != null && !string.IsNullOrEmpty(entry.fileId)
                    ? entry.fileId
                    : "file_" + (i + 1);

                item.Setup(fileId, fileName, shouldDelete, GetSprite(i, entry), canPlayToday, showDeleted && shouldDelete, ToggleFileSelection);
            }
        }

        private FileEntry GetEntry(int index)
        {
            if (todayFileSet == null || todayFileSet.files == null)
                return null;

            if (index < 0 || index >= todayFileSet.files.Length)
                return null;

            return todayFileSet.files[index];
        }

        private Sprite GetSprite(int index, FileEntry entry)
        {
            if (fileSprites == null || fileSprites.Length == 0)
                return null;

            if (entry != null && entry.spriteIndex >= 0 && entry.spriteIndex < fileSprites.Length)
                return fileSprites[entry.spriteIndex];

            int day = EmployeeHandbook.DailyTasks.GameManager.Instance != null && EmployeeHandbook.DailyTasks.GameManager.Instance.CurrentState != null
                ? EmployeeHandbook.DailyTasks.GameManager.Instance.CurrentState.currentDay
                : 1;
            int spriteIndex = Mathf.Abs(day - 1 + index) % fileSprites.Length;
            return fileSprites[spriteIndex];
        }

        private string GetFallbackFileName(int index)
        {
            if (todayDeleteTask == null || string.IsNullOrEmpty(todayDeleteTask.taskName))
                return "文件" + (index + 1);

            return todayDeleteTask.taskName + (index + 1);
        }

        private void ToggleFileSelection(FeishuDeleteFileItem item)
        {
            if (item == null)
                return;

            bool shouldSelect = !item.Selected;
            item.SetSelected(shouldSelect);

            if (shouldSelect)
            {
                if (!selectedItems.Contains(item))
                    selectedItems.Add(item);
            }
            else
            {
                selectedItems.Remove(item);
            }
        }

        private bool IsSelectionCorrect()
        {
            for (int i = 0; i < fileItems.Length; i++)
            {
                FeishuDeleteFileItem item = fileItems[i];
                if (item == null)
                    continue;

                if (item.Selected != item.ShouldDelete)
                    return false;
            }

            return true;
        }

        private void ClearSelection()
        {
            selectedItems.Clear();

            if (fileItems == null)
                return;

            for (int i = 0; i < fileItems.Length; i++)
            {
                if (fileItems[i] != null)
                    fileItems[i].SetSelected(false);
            }
        }

        private void UnlockSuccessClues()
        {
            if (clueList == null || todayFileSet == null || todayFileSet.clueIdsOnSuccess == null)
                return;

            for (int i = 0; i < todayFileSet.clueIdsOnSuccess.Length; i++)
            {
                int clueId = todayFileSet.clueIdsOnSuccess[i];
                if (clueId > 0)
                    clueList.UnlockClue(clueId);
            }
        }

        private void ShowResult(string message)
        {
            if (resultTmpText != null)
                resultTmpText.text = message;

            if (resultText != null)
                resultText.text = message;

            if (resultPopup != null)
            {
                if (resultPopupRoutine != null)
                    StopCoroutine(resultPopupRoutine);

                resultPopupRoutine = StartCoroutine(PlayResultPopup());
            }
        }

        private void CacheResultPopup()
        {
            if (resultPopup == null)
                return;

            resultPopupBaseScale = resultPopup.transform.localScale;
            resultPopupCanvasGroup = resultPopup.GetComponent<CanvasGroup>();
            if (resultPopupCanvasGroup == null)
                resultPopupCanvasGroup = resultPopup.AddComponent<CanvasGroup>();
        }

        private IEnumerator PlayResultPopup()
        {
            if (resultPopup == null)
                yield break;

            if (resultPopupCanvasGroup == null)
                CacheResultPopup();

            resultPopup.SetActive(true);
            if (resultPopupCanvasGroup != null)
            {
                resultPopupCanvasGroup.alpha = 0f;
                resultPopupCanvasGroup.interactable = false;
                resultPopupCanvasGroup.blocksRaycasts = false;
            }

            yield return AnimatePopup(0f, 1f, popupStartScale, popupOvershootScale, popupFadeInDuration);
            yield return AnimatePopup(1f, 1f, popupOvershootScale, 1f, popupFadeInDuration);

            if (popupVisibleDuration > 0f)
                yield return new WaitForSeconds(popupVisibleDuration);

            yield return AnimatePopup(1f, 0f, 1f, popupStartScale, popupFadeOutDuration);
            HideResultPopupImmediately();
        }

        private IEnumerator AnimatePopup(float fromAlpha, float toAlpha, float fromScale, float toScale, float duration)
        {
            if (duration <= 0f)
            {
                ApplyPopupFrame(toAlpha, toScale);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = 1f - Mathf.Pow(1f - t, 3f);
                ApplyPopupFrame(Mathf.Lerp(fromAlpha, toAlpha, t), Mathf.Lerp(fromScale, toScale, t));
                yield return null;
            }

            ApplyPopupFrame(toAlpha, toScale);
        }

        private void ApplyPopupFrame(float alpha, float scale)
        {
            if (resultPopupCanvasGroup != null)
                resultPopupCanvasGroup.alpha = alpha;

            if (resultPopup != null)
                resultPopup.transform.localScale = resultPopupBaseScale * scale;
        }

        private void HideResultPopupImmediately()
        {
            if (resultPopupRoutine != null)
            {
                StopCoroutine(resultPopupRoutine);
                resultPopupRoutine = null;
            }

            if (resultPopupCanvasGroup == null)
                CacheResultPopup();

            if (resultPopupCanvasGroup != null)
                resultPopupCanvasGroup.alpha = 0f;

            if (resultPopup != null)
            {
                resultPopup.transform.localScale = resultPopupBaseScale;
                resultPopup.SetActive(false);
            }
        }

        [Serializable]
        private class TaskFileSetWrapper
        {
            public TaskFileSet[] taskFileSets;
            public DayFileSet[] dayFileSets;
        }

        [Serializable]
        private class DayFileSet
        {
            public int day;
            public FileEntry[] files = new FileEntry[6];
        }

        [Serializable]
        private class TaskFileSet
        {
            [SerializeField] private string deleteFolderTaskName;
            [SerializeField] private bool useContainsMatch;
            public FileEntry[] files = new FileEntry[6];
            public int[] clueIdsOnSuccess;

            public static TaskFileSet Create(string taskName, FileEntry[] entries)
            {
                TaskFileSet set = new TaskFileSet();
                set.deleteFolderTaskName = taskName;
                set.files = entries;
                return set;
            }

            public bool Matches(string taskName)
            {
                if (string.IsNullOrEmpty(deleteFolderTaskName) || string.IsNullOrEmpty(taskName))
                    return false;

                if (useContainsMatch)
                    return taskName.Contains(deleteFolderTaskName);

                return taskName == deleteFolderTaskName;
            }
        }

        [Serializable]
        private class FileEntry
        {
            public string fileId;
            public string fileName;
            public bool shouldDelete;
            public int spriteIndex = -1;

            public static FileEntry Create(string id, string name, bool targetShouldDelete)
            {
                FileEntry entry = new FileEntry();
                entry.fileId = id;
                entry.fileName = name;
                entry.shouldDelete = targetShouldDelete;
                entry.spriteIndex = -1;
                return entry;
            }
        }
    }
}
