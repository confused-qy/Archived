using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmployeeHandbook.Feishu
{
    [RequireComponent(typeof(Button))]
    public class FeishuConversationButton : MonoBehaviour
    {
        [SerializeField] private string conversationId;
        [SerializeField] private FeishuConversationManager manager;
        [SerializeField] private Button button;
        [SerializeField] private Text contactNameText;
        [SerializeField] private TMP_Text contactNameTmpText;
        [SerializeField] private bool hideWhenLocked = true;
        [SerializeField] private bool autoBindButton = true;

        public string ConversationId
        {
            get { return conversationId; }
        }

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (contactNameText == null)
                contactNameText = GetComponentInChildren<Text>(true);

            if (contactNameTmpText == null)
                contactNameTmpText = GetComponentInChildren<TMP_Text>(true);

            if (manager == null)
                manager = GetComponentInParent<FeishuConversationManager>(true);

            if (autoBindButton && button != null)
            {
                button.onClick.RemoveListener(OpenConversation);
                button.onClick.AddListener(OpenConversation);
            }
        }

        public void Refresh(FeishuConversationManager owner)
        {
            if (owner != null)
                manager = owner;

            if (manager == null)
                return;

            string contactName = manager.GetContactName(conversationId);
            if (!string.IsNullOrWhiteSpace(contactName))
                SetContactName(contactName);

            bool unlocked = manager.IsConversationUnlocked(conversationId);
            if (hideWhenLocked)
                gameObject.SetActive(unlocked);
            else if (button != null)
                button.interactable = unlocked;
        }

        public void OpenConversation()
        {
            if (manager == null)
            {
                Debug.LogWarning("FeishuConversationButton 缺少 Conversation Manager。", this);
                return;
            }

            manager.OpenConversation(conversationId);
        }

        private void SetContactName(string value)
        {
            if (contactNameText != null)
                contactNameText.text = value;

            if (contactNameTmpText != null)
                contactNameTmpText.text = value;
        }
    }
}
