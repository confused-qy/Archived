using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmployeeHandbook.Feishu
{
    [RequireComponent(typeof(Button))]
    public class FeishuContactButton : MonoBehaviour
    {
        [SerializeField] private string contactName;
        [SerializeField] private FeishuConversationManager manager;
        [SerializeField] private Button button;
        [SerializeField] private GameObject unreadDotObject;
        [SerializeField] private Text contactNameText;
        [SerializeField] private TMP_Text contactNameTmpText;
        [SerializeField] private bool hideUntilFirstConversationUnlocked = true;
        [SerializeField] private bool autoBindButton = true;

        public string ContactName
        {
            get { return contactName; }
        }

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (manager == null)
                manager = GetComponentInParent<FeishuConversationManager>(true);

            if (contactNameText == null)
                contactNameText = GetComponentInChildren<Text>(true);

            if (contactNameTmpText == null)
                contactNameTmpText = GetComponentInChildren<TMP_Text>(true);

            if (string.IsNullOrWhiteSpace(contactName))
                contactName = GetCurrentLabel();

            if (autoBindButton && button != null)
            {
                button.onClick.RemoveListener(OpenContact);
                button.onClick.AddListener(OpenContact);
            }
        }

        private void OnEnable()
        {
            Refresh(manager);
        }

        public void Refresh(FeishuConversationManager owner)
        {
            if (owner != null)
                manager = owner;

            if (manager == null)
                return;

            bool unlocked = manager.IsContactUnlocked(contactName);
            if (hideUntilFirstConversationUnlocked)
                gameObject.SetActive(unlocked);
            else if (button != null)
                button.interactable = unlocked;

            if (unreadDotObject != null)
                unreadDotObject.SetActive(unlocked && manager.HasUnread(contactName));
        }

        public void OpenContact()
        {
            FeishuSfxPlayer.PlayOpenChatSfx();

            if (manager == null)
            {
                Debug.LogWarning("FeishuContactButton 缺少 Conversation Manager。", this);
                return;
            }

            manager.OpenContact(contactName);
        }

        private string GetCurrentLabel()
        {
            if (contactNameTmpText != null)
                return contactNameTmpText.text;

            if (contactNameText != null)
                return contactNameText.text;

            return gameObject.name;
        }
    }
}
