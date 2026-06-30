using System;
using System.Collections.Generic;

namespace EmployeeHandbook.DailyTasks
{
    /// <summary>
    /// 玩家存档状态。只保存需要跨游戏会话保留的数据。
    /// </summary>
    [Serializable]
    public class PlayerState
    {
        public int currentDay = 1;
        public int experience = 0;
        public List<string> completedTaskIds = new List<string>();
    }
}
