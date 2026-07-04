using TMPro;
using EmployeeHandbook.Feishu;
using UnityEngine;
using UnityEngine.UI;

namespace EmployeeHandbook.Phone
{
    [RequireComponent(typeof(Button))]
    public class BossPhoneChoiceButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Text labelText;
        [SerializeField] private TMP_Text labelTmpText;

        private BossPhoneCallController controller;
        private BossPhoneChoiceData choice;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (labelText == null)
                labelText = GetComponentInChildren<Text>(true);

            if (labelTmpText == null)
                labelTmpText = GetComponentInChildren<TMP_Text>(true);
        }

        public void Initialize(BossPhoneCallController owner, BossPhoneChoiceData choiceData)
        {
            controller = owner;
            choice = choiceData;
            SetText(choice != null ? choice.text : string.Empty);

            if (button == null)
                button = GetComponent<Button>();

            if (button != null)
            {
                button.onClick.RemoveListener(Choose);
                button.onClick.AddListener(Choose);
            }
        }

        private void Choose()
        {
            if (controller != null)
                controller.PlayDialogueClickSound();
            else
                FeishuSfxPlayer.PlaySendMessageSfx();

            if (controller != null && choice != null)
                controller.Choose(choice);
        }

        private void SetText(string value)
        {
            if (labelText != null)
                labelText.text = value;

            if (labelTmpText != null)
                labelTmpText.text = value;
        }
    }
}
