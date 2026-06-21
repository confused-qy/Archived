using UnityEngine;

namespace EmployeeHandbook
{
    /// <summary>Editable data for one office task or suspicious action.</summary>
    [CreateAssetMenu(fileName = "WorkTask", menuName = "Employee Handbook/Work Task")]
    public class WorkTaskData : ScriptableObject
    {
        [Header("Display")]
        public string title;
        [TextArea(2, 5)] public string description;
        public string buttonText = "Complete";

        [Header("Results")]
        public int complianceChange;
        public int autonomyChange;
        [TextArea(2, 4)] public string optionalClueUnlock;

        [Header("Warning")]
        public bool requiresWarning;
        public string warningTitle = "Work Scope Warning";
        [TextArea(2, 5)] public string warningMessage;
    }
}
