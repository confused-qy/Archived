using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmployeeHandbook.Feishu
{
    [RequireComponent(typeof(Button))]
    public class FeishuChoiceButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Text labelText;
        [SerializeField] private TMP_Text labelTmpText;

        private FeishuChatController chatController;
        private FeishuContactChatController contactChatController;
        private FeishuChoiceData choice;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (labelText == null)
                labelText = GetComponentInChildren<Text>(true);

            if (labelTmpText == null)
                labelTmpText = GetComponentInChildren<TMP_Text>(true);
        }

        public void Initialize(FeishuChatController controller, FeishuChoiceData choiceData)
        {
            chatController = controller;
            contactChatController = null;
            InitializeChoice(choiceData);
        }

        public void Initialize(FeishuContactChatController controller, FeishuChoiceData choiceData)
        {
            chatController = null;
            contactChatController = controller;
            InitializeChoice(choiceData);
        }

        private void InitializeChoice(FeishuChoiceData choiceData)
        {
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
            if (chatController != null && choice != null)
            {
                chatController.Choose(choice);
                return;
            }

            if (contactChatController != null && choice != null)
                contactChatController.Choose(choice);
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
