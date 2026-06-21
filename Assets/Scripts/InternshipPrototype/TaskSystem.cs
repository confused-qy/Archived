using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmployeeHandbook
{
    /// <summary>Runs the ordered office-task list and applies task rewards.</summary>
    public class TaskSystem : MonoBehaviour
    {
        [SerializeField] private List<WorkTaskData> stageOneTasks = new List<WorkTaskData>();
        [SerializeField] private WorkTaskData suspiciousEmailTask;
        [SerializeField] private int suspiciousEmailAppearsAfter = 2;
        [SerializeField] private int tasksRequiredForPromotion = 5;

        private int currentTaskIndex;
        private bool suspiciousEmailRevealed;
        private bool suspiciousEmailCompleted;
        private bool promotionRequested;

        public WorkTaskData CurrentTask => currentTaskIndex < stageOneTasks.Count
            ? stageOneTasks[currentTaskIndex]
            : null;
        public WorkTaskData SuspiciousEmailTask => suspiciousEmailTask;

        public event Action TaskChanged;
        public event Action SuspiciousEmailRevealed;
        public event Action PromotionRequested;

        private void Start()
        {
            TaskChanged?.Invoke();
        }

        public void CompleteCurrentTask(WorkTaskData task)
        {
            if (task == null || task != CurrentTask)
                return;

            ApplyResults(task);
            currentTaskIndex++;

            if (!suspiciousEmailRevealed && GameManager.Instance.CompletedTaskCount >= suspiciousEmailAppearsAfter)
            {
                suspiciousEmailRevealed = true;
                SuspiciousEmailRevealed?.Invoke();
            }

            TaskChanged?.Invoke();
            CheckForPromotion();
        }

        public void CompleteSuspiciousAction(WorkTaskData task)
        {
            if (task == null || task != suspiciousEmailTask || suspiciousEmailCompleted)
                return;

            ApplyScoreAndClueResults(task);
            suspiciousEmailCompleted = true;
            TaskChanged?.Invoke();
        }

        private static void ApplyResults(WorkTaskData task)
        {
            ApplyScoreAndClueResults(task);
            GameManager.Instance.CompleteTask();
        }

        private static void ApplyScoreAndClueResults(WorkTaskData task)
        {
            GameManager.Instance.AddCompliance(task.complianceChange);
            GameManager.Instance.AddAutonomy(task.autonomyChange);
            GameManager.Instance.UnlockClue(task.optionalClueUnlock);
        }

        private void CheckForPromotion()
        {
            if (promotionRequested || GameManager.Instance.CompletedTaskCount < tasksRequiredForPromotion)
                return;

            promotionRequested = true;
            PromotionRequested?.Invoke();
        }
    }
}
