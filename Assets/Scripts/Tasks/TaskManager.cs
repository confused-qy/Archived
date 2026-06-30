using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmployeeHandbook.DailyTasks
{
    /// <summary>
    /// 负责加载每日任务、生成当天 3 个任务，并接收小游戏成功结果。
    /// </summary>
    public class TaskManager : MonoBehaviour
    {
        [SerializeField] private string tasksResourcePath = "tasks";
        [SerializeField] private int tasksPerDay = 3;

        private readonly List<TaskData> allTasks = new List<TaskData>();
        private PlayerState playerState;

        public event Action TasksChanged;

        [Serializable]
        private class TaskJsonData
        {
            public int day;
            public string taskId;
            public string taskName;
            public string taskType;
            public bool required;
        }

        [Serializable]
        private class TaskListWrapper
        {
            public List<TaskJsonData> tasks = new List<TaskJsonData>();
        }

        private void Awake()
        {
            LoadAllTasks();
        }

        public void Initialize(PlayerState state)
        {
            playerState = state;

            if (playerState == null)
            {
                Debug.LogWarning("TaskManager 初始化失败：PlayerState 为空。");
                return;
            }

            if (playerState.completedTaskIds == null)
                playerState.completedTaskIds = new List<string>();

            RefreshCompletedFlags();
            Debug.Log("TaskManager 初始化完成：当前第 " + playerState.currentDay + " 天。");
            TasksChanged?.Invoke();
        }

        public void LoadAllTasks()
        {
            allTasks.Clear();

            TextAsset jsonAsset = Resources.Load<TextAsset>(tasksResourcePath);
            if (jsonAsset == null)
            {
                Debug.LogWarning("未找到任务表 Resources/" + tasksResourcePath + ".json，请确认文件路径。");
                return;
            }

            TaskListWrapper wrapper = JsonUtility.FromJson<TaskListWrapper>(jsonAsset.text);
            if (wrapper == null || wrapper.tasks == null)
            {
                Debug.LogWarning("任务表解析失败，请检查 JSON 格式。");
                return;
            }

            for (int i = 0; i < wrapper.tasks.Count; i++)
            {
                TaskData task = ConvertJsonTask(wrapper.tasks[i]);
                if (task != null)
                    allTasks.Add(task);
            }

            Debug.Log("任务表加载完成，共 " + allTasks.Count + " 条任务。");
        }

        public List<TaskData> GetTodayTasks()
        {
            List<TaskData> todayTasks = new List<TaskData>();

            if (playerState == null)
            {
                Debug.LogWarning("无法获取当天任务：PlayerState 为空。");
                return todayTasks;
            }

            RefreshCompletedFlags();

            for (int i = 0; i < allTasks.Count; i++)
            {
                if (allTasks[i] != null && allTasks[i].day == playerState.currentDay)
                {
                    todayTasks.Add(allTasks[i]);

                    if (todayTasks.Count >= tasksPerDay)
                        break;
                }
            }

            if (todayTasks.Count < tasksPerDay)
                Debug.LogWarning("第 " + playerState.currentDay + " 天任务不足 " + tasksPerDay + " 个，当前只有 " + todayTasks.Count + " 个。");

            return todayTasks;
        }

        public bool CompleteTaskByMiniGame(TaskType taskType)
        {
            if (playerState == null)
            {
                Debug.LogWarning("小游戏完成上报失败：PlayerState 为空。");
                return false;
            }

            List<TaskData> todayTasks = GetTodayTasks();
            for (int i = 0; i < todayTasks.Count; i++)
            {
                TaskData task = todayTasks[i];
                if (task == null || task.taskType != taskType)
                    continue;

                if (playerState.completedTaskIds.Contains(task.taskId))
                {
                    Debug.Log("小游戏成功，但今天这个类型的任务已经完成：" + task.taskId);
                    continue;
                }

                return CompleteTask(task.taskId);
            }

            Debug.Log("小游戏成功，但今天没有需要完成的 " + taskType + " 任务。");
            return false;
        }

        public bool CompleteTaskByMiniGame(string taskId)
        {
            if (string.IsNullOrEmpty(taskId))
            {
                Debug.LogWarning("小游戏完成上报失败：taskId 为空。");
                return false;
            }

            return CompleteTask(taskId);
        }

        private bool CompleteTask(string taskId)
        {
            if (playerState == null)
            {
                Debug.LogWarning("完成任务失败：PlayerState 为空。");
                return false;
            }

            if (string.IsNullOrEmpty(taskId))
            {
                Debug.LogWarning("完成任务失败：taskId 为空。");
                return false;
            }

            TaskData task = FindTaskById(taskId);
            if (task == null)
            {
                Debug.LogWarning("完成任务失败：找不到任务 " + taskId);
                return false;
            }

            if (!IsTodayGeneratedTask(task))
            {
                Debug.Log("小游戏成功，但任务 " + taskId + " 不是今天 DailyTasks UI 里的任务，不打勾。");
                return false;
            }

            if (playerState.completedTaskIds.Contains(taskId))
            {
                Debug.Log("任务已经完成，忽略重复完成：" + taskId);
                return false;
            }

            playerState.completedTaskIds.Add(taskId);
            task.completed = true;

            Debug.Log("完成任务：" + task.taskName);
            TasksChanged?.Invoke();
            return true;
        }

        public bool AreTodayRequiredTasksCompleted()
        {
            if (playerState == null)
            {
                Debug.LogWarning("无法判断必做任务：PlayerState 为空。");
                return false;
            }

            List<TaskData> todayTasks = GetTodayTasks();
            if (todayTasks.Count == 0)
            {
                Debug.LogWarning("今天没有生成任务，不能进入下一天。");
                return false;
            }

            for (int i = 0; i < todayTasks.Count; i++)
            {
                TaskData task = todayTasks[i];
                if (task != null && task.required && !playerState.completedTaskIds.Contains(task.taskId))
                    return false;
            }

            return true;
        }

        public bool GoToNextDay()
        {
            if (playerState == null)
            {
                Debug.LogWarning("进入下一天失败：PlayerState 为空。");
                return false;
            }

            if (!AreTodayRequiredTasksCompleted())
            {
                Debug.LogWarning("还有必做任务未完成，不能进入下一天。");
                return false;
            }

            playerState.currentDay++;
            RefreshCompletedFlags();
            Debug.Log("进入第 " + playerState.currentDay + " 天。");
            TasksChanged?.Invoke();
            return true;
        }

        public TaskData FindTaskById(string taskId)
        {
            if (string.IsNullOrEmpty(taskId))
                return null;

            for (int i = 0; i < allTasks.Count; i++)
            {
                TaskData task = allTasks[i];
                if (task != null && task.taskId == taskId)
                    return task;
            }

            return null;
        }

        private bool IsTodayGeneratedTask(TaskData targetTask)
        {
            if (targetTask == null)
                return false;

            List<TaskData> todayTasks = GetTodayTasks();
            for (int i = 0; i < todayTasks.Count; i++)
            {
                TaskData task = todayTasks[i];
                if (task != null && task.taskId == targetTask.taskId)
                    return true;
            }

            return false;
        }

        private void RefreshCompletedFlags()
        {
            if (playerState == null || playerState.completedTaskIds == null)
                return;

            for (int i = 0; i < allTasks.Count; i++)
            {
                TaskData task = allTasks[i];
                if (task != null)
                    task.completed = playerState.completedTaskIds.Contains(task.taskId);
            }
        }

        private TaskData ConvertJsonTask(TaskJsonData jsonTask)
        {
            if (jsonTask == null)
                return null;

            TaskType parsedType = TaskType.EmailRead;
            try
            {
                parsedType = (TaskType)Enum.Parse(typeof(TaskType), jsonTask.taskType);
            }
            catch
            {
                Debug.LogWarning("任务 " + jsonTask.taskId + " 的 taskType 无法识别：" + jsonTask.taskType + "，将使用 EmailRead。");
            }

            TaskData task = new TaskData();
            task.day = jsonTask.day;
            task.taskId = jsonTask.taskId;
            task.taskName = jsonTask.taskName;
            task.taskType = parsedType;
            task.required = jsonTask.required;
            task.completed = false;
            return task;
        }
    }
}
