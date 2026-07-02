using UnityEngine;

namespace EmployeeHandbook.DailyTasks
{
    /// <summary>
    /// 根据当前天数显示日历上的斜杠。
    /// Day 1 显示 0 个斜杠，Day 2 显示 1 个斜杠，超过 Day 20 显示 20 个斜杠。
    /// </summary>
    public class CalendarProgressUI : MonoBehaviour
    {
        [SerializeField] private GameObject[] slashObjects = new GameObject[GameManager.TotalGameDays];
        [SerializeField] private bool autoFindSlashObjects = true;
        [SerializeField] private string slashObjectNamePrefix = "Slash";

        private void Start()
        {
            AutoBindSlashObjects();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.StateChanged += Refresh;
                Refresh();
            }
            else
            {
                Debug.LogWarning("CalendarProgressUI 没有找到 DailyTasks.GameManager。", this);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.StateChanged -= Refresh;
        }

        public void Refresh()
        {
            AutoBindSlashObjects();

            int currentDay = 1;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != null)
                currentDay = GameManager.Instance.CurrentState.currentDay;

            int completedDays = Mathf.Clamp(currentDay - 1, 0, Mathf.Min(slashObjects.Length, GameManager.TotalGameDays));

            for (int i = 0; i < slashObjects.Length; i++)
            {
                if (slashObjects[i] == null)
                    continue;

                slashObjects[i].SetActive(i < completedDays);
            }

            Debug.Log("日历进度已刷新：当前第 " + currentDay + " 天，划掉 " + completedDays + " 天。");
        }

        private void AutoBindSlashObjects()
        {
            if (!autoFindSlashObjects || slashObjects == null)
                return;

            for (int i = 0; i < slashObjects.Length; i++)
            {
                if (slashObjects[i] != null)
                    continue;

                Transform slashTransform = transform.Find(slashObjectNamePrefix + (i + 1));
                if (slashTransform != null)
                    slashObjects[i] = slashTransform.gameObject;
            }
        }
    }
}
