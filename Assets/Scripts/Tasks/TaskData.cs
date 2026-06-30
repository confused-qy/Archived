using System;
using UnityEngine;

namespace EmployeeHandbook.DailyTasks
{
    /// <summary>
    /// 单条每日任务数据。completed 会根据玩家存档动态刷新。
    /// </summary>
    [Serializable]
    public class TaskData
    {
        public int day;
        public string taskId;
        public string taskName;
        public TaskType taskType;
        public bool required;
        public bool completed;
    }
}
