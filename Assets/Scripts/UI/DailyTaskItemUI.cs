using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EmployeeHandbook.DailyTasks
{
    /// <summary>
    /// DailyTasks 面板中的单条任务显示。这里只显示状态，不判定完成。
    /// </summary>
    public class DailyTaskItemUI : MonoBehaviour
    {
        [SerializeField] private Image checkImage;
        [SerializeField] private Sprite uncheckedSprite;
        [SerializeField] private Sprite checkedSprite;
        [SerializeField] private Text taskNameText;
        [SerializeField] private TMP_Text taskNameTmpText;

        private void Awake()
        {
            AutoBindChildren();
        }

        public void SetSprites(Sprite uncheckedIcon, Sprite checkedIcon)
        {
            uncheckedSprite = uncheckedIcon;
            checkedSprite = checkedIcon;
        }

        public void Setup(TaskData taskData)
        {
            AutoBindChildren();

            if (taskData == null)
            {
                Clear();
                return;
            }

            SetTaskName(taskData.taskName);

            if (checkImage != null)
                checkImage.sprite = taskData.completed ? checkedSprite : uncheckedSprite;
            else
                Debug.LogWarning("DailyTaskItemUI 缺少 Check Image 引用。", this);

            gameObject.SetActive(true);
        }

        public void Clear()
        {
            SetTaskName("");

            if (checkImage != null)
                checkImage.sprite = uncheckedSprite;

            gameObject.SetActive(false);
        }

        private void AutoBindChildren()
        {
            if (checkImage == null)
            {
                Transform checkIcon = transform.Find("CheckIcon");
                if (checkIcon != null)
                    checkImage = checkIcon.GetComponent<Image>();
            }

            if (taskNameText == null)
            {
                Transform taskName = transform.Find("TaskNameText");
                if (taskName != null)
                {
                    taskNameText = taskName.GetComponent<Text>();
                    taskNameTmpText = taskName.GetComponent<TMP_Text>();
                }
            }
        }

        private void SetTaskName(string value)
        {
            if (taskNameText != null)
                taskNameText.text = value;

            if (taskNameTmpText != null)
                taskNameTmpText.text = value;
        }
    }
}
