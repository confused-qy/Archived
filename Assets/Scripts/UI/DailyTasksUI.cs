using System.Collections.Generic;
using UnityEngine;

namespace EmployeeHandbook.DailyTasks
{
    /// <summary>
    /// DailyTasks 面板。使用固定的 TaskSlot1/2/3 显示当天 3 个任务，不动态生成 prefab。
    /// </summary>
    public class DailyTasksUI : MonoBehaviour
    {
        [SerializeField] private TaskManager taskManager;
        [SerializeField] private Sprite uncheckedSprite;
        [SerializeField] private Sprite checkedSprite;
        [SerializeField] private DailyTaskItemUI taskSlot1;
        [SerializeField] private DailyTaskItemUI taskSlot2;
        [SerializeField] private DailyTaskItemUI taskSlot3;

        private DailyTaskItemUI[] taskSlots;

        private void Start()
        {
            AutoBindPanel();

            if (taskManager == null && GameManager.Instance != null)
                taskManager = GameManager.Instance.TaskManager;

            if (taskManager == null)
                taskManager = FindObjectOfType<TaskManager>();

            if (taskManager != null)
                taskManager.TasksChanged += Refresh;
            else
                Debug.LogWarning("DailyTasksUI 没有找到 TaskManager。", this);

            Refresh();
        }

        private void OnDestroy()
        {
            if (taskManager != null)
                taskManager.TasksChanged -= Refresh;
        }

        public void Refresh()
        {
            AutoBindPanel();

            if (taskManager == null)
            {
                Debug.LogWarning("刷新 DailyTasks 失败：TaskManager 为空。", this);
                return;
            }

            List<TaskData> todayTasks = taskManager.GetTodayTasks();
            for (int i = 0; i < taskSlots.Length; i++)
            {
                DailyTaskItemUI slot = taskSlots[i];
                if (slot == null)
                {
                    Debug.LogWarning("DailyTasksUI 缺少 TaskSlot" + (i + 1) + " 引用。", this);
                    continue;
                }

                slot.SetSprites(uncheckedSprite, checkedSprite);

                if (i < todayTasks.Count)
                    slot.Setup(todayTasks[i]);
                else
                    slot.Clear();
            }

            Debug.Log("DailyTasks UI 已刷新，显示任务数量：" + todayTasks.Count);
        }

        private void AutoBindPanel()
        {
            if (taskSlot1 == null)
                taskSlot1 = FindOrAddSlot("TaskSlot1");

            if (taskSlot2 == null)
                taskSlot2 = FindOrAddSlot("TaskSlot2");

            if (taskSlot3 == null)
                taskSlot3 = FindOrAddSlot("TaskSlot3");

            taskSlots = new DailyTaskItemUI[] { taskSlot1, taskSlot2, taskSlot3 };
        }

        private DailyTaskItemUI FindOrAddSlot(string slotName)
        {
            Transform slotTransform = transform.Find(slotName);
            if (slotTransform == null)
            {
                Debug.LogWarning("DailyTasksUI 找不到 " + slotName + "，请检查 DailyTasksPanel 层级命名。", this);
                return null;
            }

            DailyTaskItemUI slot = slotTransform.GetComponent<DailyTaskItemUI>();
            if (slot == null)
                slot = slotTransform.gameObject.AddComponent<DailyTaskItemUI>();

            return slot;
        }
    }
}
