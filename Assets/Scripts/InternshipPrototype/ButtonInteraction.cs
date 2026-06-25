using System;
using UnityEngine;
using UnityEngine.UI;

namespace EmployeeHandbook
{
    /// <summary>Connects any UGUI Button to a task, with optional confirmation.</summary>
    [RequireComponent(typeof(Button))]
    public class ButtonInteraction : MonoBehaviour
    {
        private const float DoubleClickWindow = 0.35f;

        [SerializeField] private Button button;
        [SerializeField] private Text buttonLabel;
        [SerializeField] private WarningDialog warningDialog;

        private WorkTaskData task;
        private Action<WorkTaskData> action;
        private float lastClickTime = -1f;

        private void Reset()
        {
            button = GetComponent<Button>();
            buttonLabel = GetComponentInChildren<Text>();
        }

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();

            button.onClick.AddListener(HandleClick);
        }

        public void Configure(WorkTaskData newTask, Action<WorkTaskData> clickAction)
        {
            task = newTask;
            action = clickAction;
            button.interactable = task != null;

            if (buttonLabel != null)
                buttonLabel.text = task != null ? task.buttonText : "Unavailable";
        }

        private void HandleClick()
        {
            float clickTime = Time.unscaledTime;
            bool isDoubleClick = clickTime - lastClickTime <= DoubleClickWindow;
            lastClickTime = clickTime;

            if (!isDoubleClick)
                return;

            lastClickTime = -1f;

            if (task == null || action == null)
                return;

            if (task.requiresWarning && warningDialog != null)
            {
                warningDialog.Show(task.warningTitle, task.warningMessage, Execute);
                return;
            }

            Execute();
        }

        private void Execute()
        {
            action?.Invoke(task);
        }
    }
}
