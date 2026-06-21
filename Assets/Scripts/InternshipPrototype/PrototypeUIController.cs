using UnityEngine;
using UnityEngine.UI;

namespace EmployeeHandbook
{
    /// <summary>Updates the desktop UI and connects it to the task flow.</summary>
    public class PrototypeUIController : MonoBehaviour
    {
        private const string PromotionMessage =
            "Congratulations. You have passed the internship screening. Your stability performance is good. Permission level upgraded.";

        [Header("Systems")]
        [SerializeField] private TaskSystem taskSystem;
        [SerializeField] private WarningDialog warningDialog;

        [Header("Main Work Area")]
        [SerializeField] private Text taskTitleText;
        [SerializeField] private Text taskDescriptionText;
        [SerializeField] private ButtonInteraction mainActionButton;
        [SerializeField] private GameObject suspiciousEmailPanel;
        [SerializeField] private ButtonInteraction suspiciousActionButton;

        [Header("Status Bar")]
        [SerializeField] private Text complianceText;
        [SerializeField] private Text autonomyText;
        [SerializeField] private Text stageText;

        private void Start()
        {
            // This controller belongs to the complete prototype UI. If that UI has
            // not been wired in the Inspector, disable it instead of throwing a
            // NullReferenceException and disturbing simpler scene experiments.
            if (!HasRequiredReferences())
            {
                Debug.LogWarning(
                    "PrototypeUIController is not configured, so it has been disabled. " +
                    "You can safely remove it from UIManager while using the simple window prototype.",
                    this);
                enabled = false;
                return;
            }

            taskSystem.TaskChanged += RefreshTask;
            taskSystem.SuspiciousEmailRevealed += RevealSuspiciousEmail;
            taskSystem.PromotionRequested += ShowPromotion;
            GameManager.Instance.StateChanged += RefreshStatus;

            suspiciousEmailPanel.SetActive(false);
            RefreshTask();
            RefreshStatus();
        }

        private bool HasRequiredReferences()
        {
            return GameManager.Instance != null &&
                   taskSystem != null &&
                   warningDialog != null &&
                   taskTitleText != null &&
                   taskDescriptionText != null &&
                   mainActionButton != null &&
                   suspiciousEmailPanel != null &&
                   suspiciousActionButton != null &&
                   complianceText != null &&
                   autonomyText != null &&
                   stageText != null;
        }

        private void OnDestroy()
        {
            if (taskSystem != null)
            {
                taskSystem.TaskChanged -= RefreshTask;
                taskSystem.SuspiciousEmailRevealed -= RevealSuspiciousEmail;
                taskSystem.PromotionRequested -= ShowPromotion;
            }

            if (GameManager.Instance != null)
                GameManager.Instance.StateChanged -= RefreshStatus;
        }

        private void RefreshTask()
        {
            WorkTaskData task = taskSystem.CurrentTask;
            if (task == null)
            {
                taskTitleText.text = "All assigned work complete";
                taskDescriptionText.text = "Please wait for your evaluation result.";
            }
            else
            {
                taskTitleText.text = task.title;
                taskDescriptionText.text = task.description;
            }

            mainActionButton.Configure(task, taskSystem.CompleteCurrentTask);
        }

        private void RevealSuspiciousEmail()
        {
            suspiciousEmailPanel.SetActive(true);
            suspiciousActionButton.Configure(taskSystem.SuspiciousEmailTask, OpenSuspiciousEmail);
        }

        private void OpenSuspiciousEmail(WorkTaskData task)
        {
            taskSystem.CompleteSuspiciousAction(task);
            suspiciousEmailPanel.SetActive(false);
        }

        private void RefreshStatus()
        {
            complianceText.text = $"Compliance: {GameManager.Instance.Compliance}";
            autonomyText.text = $"Autonomy: {GameManager.Instance.Autonomy}";
            stageText.text = GameManager.Instance.CurrentStage == 1
                ? "Stage 1: Internship Trial Period"
                : "Stage 2: Placeholder";
        }

        private void ShowPromotion()
        {
            warningDialog.ShowMessage("Permission Upgrade", PromotionMessage, EnterStageTwo);
        }

        private void EnterStageTwo()
        {
            GameManager.Instance.SetStage(2);
            taskTitleText.text = "Stage 2 Placeholder";
            taskDescriptionText.text = "Further permissions are not available in this prototype.";
            mainActionButton.Configure(null, null);
            suspiciousEmailPanel.SetActive(false);
        }
    }
}
