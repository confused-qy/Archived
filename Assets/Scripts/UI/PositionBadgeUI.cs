using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmployeeHandbook.DailyTasks
{
    /// <summary>
    /// 根据当前天数刷新工牌上的职位等级文字和图片。
    /// 每 5 天提升 1 级：1-5 天为 1 级，26-30 天为 6 级。
    /// </summary>
    public class PositionBadgeUI : MonoBehaviour
    {
        [SerializeField] private Image badgeImage;
        [SerializeField] private Text positionText;
        [SerializeField] private TMP_Text positionTmpText;
        [SerializeField] private Sprite[] levelSprites = new Sprite[6];
        [SerializeField] private string[] levelNames =
        {
            "实习生",
            "助理",
            "专员",
            "主管",
            "经理",
            "总监"
        };

        private void Start()
        {
            AutoBindChildren();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.StateChanged += Refresh;
                Refresh();
            }
            else
            {
                Debug.LogWarning("PositionBadgeUI 没有找到 DailyTasks.GameManager。", this);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.StateChanged -= Refresh;
        }

        public void Refresh()
        {
            int level = 1;
            if (GameManager.Instance != null)
                level = GameManager.Instance.CurrentPositionLevel;

            int index = Mathf.Clamp(level - 1, 0, 5);

            if (badgeImage != null && levelSprites != null && index < levelSprites.Length && levelSprites[index] != null)
                badgeImage.sprite = levelSprites[index];

            SetPositionText(GetLevelName(index));
            Debug.Log("工牌职位已刷新：等级 " + level + "，" + GetLevelName(index));
        }

        private string GetLevelName(int index)
        {
            if (levelNames != null && index < levelNames.Length && !string.IsNullOrEmpty(levelNames[index]))
                return levelNames[index];

            return "等级 " + (index + 1);
        }

        private void SetPositionText(string value)
        {
            if (positionText != null)
                positionText.text = value;

            if (positionTmpText != null)
                positionTmpText.text = value;
        }

        private void AutoBindChildren()
        {
            if (badgeImage == null)
            {
                Transform imageTransform = transform.Find("BadgeImage");
                if (imageTransform != null)
                    badgeImage = imageTransform.GetComponent<Image>();
            }

            if (positionText == null && positionTmpText == null)
            {
                Transform textTransform = transform.Find("PositionText");
                if (textTransform != null)
                {
                    positionText = textTransform.GetComponent<Text>();
                    positionTmpText = textTransform.GetComponent<TMP_Text>();
                }
            }
        }
    }
}
