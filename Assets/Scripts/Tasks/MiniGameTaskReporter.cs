using UnityEngine;

namespace EmployeeHandbook.DailyTasks
{
    /// <summary>
    /// 挂在小游戏对象上。小游戏成功时调用 ReportSuccess，把结果交给每日任务系统判断。
    /// </summary>
    public class MiniGameTaskReporter : MonoBehaviour
    {
        [SerializeField] private TaskType taskType;
        [SerializeField] private string taskId;

        public void ReportSuccess()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("小游戏成功上报失败：场景中没有 DailyTasks.GameManager。", this);
                return;
            }

            if (!string.IsNullOrEmpty(taskId))
                GameManager.Instance.ReportMiniGameSuccess(taskId);
            else
                GameManager.Instance.ReportMiniGameSuccess(taskType);
        }
    }
}
