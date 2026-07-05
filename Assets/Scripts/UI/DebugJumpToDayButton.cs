using UnityEngine;

namespace EmployeeHandbook.DailyTasks
{
    /// <summary>
    /// 测试按钮：直接跳到指定天数，默认第 19 天。
    /// </summary>
    public class DebugJumpToDayButton : MonoBehaviour
    {
        [SerializeField] private int targetDay = 19;

        public void JumpToTargetDay()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("跳天失败：场景中没有 DailyTasks.GameManager。", this);
                return;
            }

            GameManager.Instance.DebugJumpToDay(targetDay);
        }

        public void JumpToDay19()
        {
            targetDay = 19;
            JumpToTargetDay();
        }
    }
}
