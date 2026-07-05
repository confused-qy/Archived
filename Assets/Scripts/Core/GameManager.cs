using System;
using UnityEngine;

namespace EmployeeHandbook.DailyTasks
{
    /// <summary>
    /// 每日任务系统的总入口：新游戏、继续游戏、完成任务、进入下一天。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public const int TotalGameDays = 20;
        public const int DaysPerPositionLevel = 4;
        public const int MaxPositionLevel = 5;

        public static GameManager Instance { get; private set; }

        [SerializeField] private TaskManager taskManager;
        [SerializeField] private bool autoContinueOrNewGame = true;
        [SerializeField] private bool saveProgress = false;
        [SerializeField] private bool resetSaveOnStart = true;

        private PlayerState playerState;

        public PlayerState CurrentState
        {
            get { return playerState; }
        }

        public TaskManager TaskManager
        {
            get { return taskManager; }
        }

        public int CurrentPositionLevel
        {
            get
            {
                if (playerState == null)
                    return 1;

                return Mathf.Clamp(((playerState.currentDay - 1) / DaysPerPositionLevel) + 1, 1, MaxPositionLevel);
            }
        }

        public event Action StateChanged;
        public event Action GameEnded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("场景中存在多个 DailyTasks.GameManager，已销毁重复对象。", this);
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (taskManager == null)
                taskManager = FindObjectOfType<TaskManager>();

            if (taskManager == null)
                Debug.LogWarning("GameManager 没有找到 TaskManager，请在 Inspector 中拖入引用。");
        }

        private void Start()
        {
            if (GameLaunchState.HasRequestedMode)
            {
                ApplyLaunchMode(GameLaunchState.ConsumeStartMode());
                return;
            }

            if (!autoContinueOrNewGame)
                return;

            if (!saveProgress || resetSaveOnStart)
            {
                SaveManager.DeleteSave();
                NewGame();
                return;
            }

            if (SaveManager.HasSave())
                ContinueGame();
            else
                NewGame();
        }

        private void ApplyLaunchMode(GameStartMode mode)
        {
            if (mode == GameStartMode.NewGame)
            {
                SaveManager.DeleteSave();
                NewGame();
                return;
            }

            if (mode == GameStartMode.ContinueGame)
            {
                if (SaveManager.HasSave())
                    ContinueGame();
                else
                    NewGame();
            }
        }

        public void NewGame()
        {
            playerState = new PlayerState();
            playerState.currentDay = 1;
            playerState.experience = 0;

            Debug.Log("开始新游戏：第 1 天，经验 0。");
            InitializeTaskManager();
            SaveCurrentState();
            StateChanged?.Invoke();
        }

        public void ContinueGame()
        {
            playerState = SaveManager.Load();
            if (playerState == null)
            {
                Debug.LogWarning("继续游戏失败，将自动开始新游戏。");
                NewGame();
                return;
            }

            Debug.Log("继续游戏：第 " + playerState.currentDay + " 天，经验 " + playerState.experience + "。");
            InitializeTaskManager();
            CheckEnding();
            StateChanged?.Invoke();
        }

        public void ReportMiniGameSuccess(TaskType taskType)
        {
            if (!EnsureReady())
                return;

            bool completed = taskManager.CompleteTaskByMiniGame(taskType);
            if (!completed)
                return;

            SaveCurrentState();
            StateChanged?.Invoke();
        }

        public void ReportMiniGameSuccess(string taskId)
        {
            if (!EnsureReady())
                return;

            bool completed = taskManager.CompleteTaskByMiniGame(taskId);
            if (!completed)
                return;

            SaveCurrentState();
            StateChanged?.Invoke();
        }

        public void NextDay()
        {
            if (!EnsureReady())
                return;

            if (!taskManager.AreTodayRequiredTasksCompleted())
            {
                Debug.LogWarning("不能进入下一天：当天必做任务尚未全部完成。");
                return;
            }

            if (!taskManager.GoToNextDay())
                return;

            SaveCurrentState();
            StateChanged?.Invoke();
            CheckEnding();
        }

        public void DebugJumpToDay(int day)
        {
            if (playerState == null)
            {
                playerState = new PlayerState();

                if (playerState.completedTaskIds == null)
                    playerState.completedTaskIds = new System.Collections.Generic.List<string>();
            }

            playerState.currentDay = Mathf.Clamp(day, 1, TotalGameDays);

            InitializeTaskManager();
            SaveCurrentState();
            StateChanged?.Invoke();
            CheckEnding();

            Debug.Log("测试跳转到第 " + playerState.currentDay + " 天。");
        }

        private void SaveCurrentState()
        {
            if (!saveProgress)
            {
                Debug.Log("测试模式：Save Progress 未开启，本次进度不会保存。");
                return;
            }

            SaveManager.Save(playerState);
        }

        private void InitializeTaskManager()
        {
            if (taskManager == null)
            {
                Debug.LogWarning("无法初始化任务系统：TaskManager 为空。");
                return;
            }

            taskManager.Initialize(playerState);
        }

        private bool EnsureReady()
        {
            if (playerState == null)
            {
                Debug.LogWarning("GameManager 尚未初始化 PlayerState，请先调用 NewGame 或 ContinueGame。");
                return false;
            }

            if (taskManager == null)
            {
                Debug.LogWarning("GameManager 缺少 TaskManager 引用。");
                return false;
            }

            return true;
        }

        private void CheckEnding()
        {
            if (playerState == null)
                return;

            if (playerState.currentDay <= TotalGameDays)
                return;

            Debug.Log("当前天数超过 " + TotalGameDays + " 天，进入结局判断。最终经验：" + playerState.experience);
            GameEnded?.Invoke();
        }
    }
}
