using UnityEngine;

namespace EmployeeHandbook.Feishu
{
    public class FeishuCalendarDayMarker : MonoBehaviour
    {
        [SerializeField] private Transform markerRoot;
        [SerializeField] private bool refreshOnEnable = true;

        private bool subscribed;

        private void Awake()
        {
            if (markerRoot == null)
                markerRoot = transform;
        }

        private void OnEnable()
        {
            Subscribe();

            if (refreshOnEnable)
                RefreshMarker();
        }

        private void Start()
        {
            Subscribe();
            RefreshMarker();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void RefreshMarker()
        {
            int currentDay = GetCurrentDay();

            if (markerRoot == null)
                return;

            for (int i = 0; i < markerRoot.childCount; i++)
            {
                Transform child = markerRoot.GetChild(i);
                if (child == null)
                    continue;

                child.gameObject.SetActive(child.name == currentDay.ToString());
            }
        }

        private int GetCurrentDay()
        {
            if (EmployeeHandbook.DailyTasks.GameManager.Instance != null && EmployeeHandbook.DailyTasks.GameManager.Instance.CurrentState != null)
                return EmployeeHandbook.DailyTasks.GameManager.Instance.CurrentState.currentDay;

            return 1;
        }

        private void Subscribe()
        {
            if (subscribed || EmployeeHandbook.DailyTasks.GameManager.Instance == null)
                return;

            EmployeeHandbook.DailyTasks.GameManager.Instance.StateChanged += RefreshMarker;

            if (EmployeeHandbook.DailyTasks.GameManager.Instance.TaskManager != null)
                EmployeeHandbook.DailyTasks.GameManager.Instance.TaskManager.TasksChanged += RefreshMarker;

            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || EmployeeHandbook.DailyTasks.GameManager.Instance == null)
                return;

            EmployeeHandbook.DailyTasks.GameManager.Instance.StateChanged -= RefreshMarker;

            if (EmployeeHandbook.DailyTasks.GameManager.Instance.TaskManager != null)
                EmployeeHandbook.DailyTasks.GameManager.Instance.TaskManager.TasksChanged -= RefreshMarker;

            subscribed = false;
        }
    }
}
