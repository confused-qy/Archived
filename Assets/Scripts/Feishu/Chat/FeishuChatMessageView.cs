using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmployeeHandbook.Feishu
{
    public class FeishuChatMessageView : MonoBehaviour
    {
        [SerializeField] private Text messageText;
        [SerializeField] private TMP_Text messageTmpText;

        public void SetText(string value)
        {
            if (messageText == null)
                messageText = GetComponentInChildren<Text>(true);

            if (messageTmpText == null)
                messageTmpText = GetComponentInChildren<TMP_Text>(true);

            if (messageText != null)
                messageText.text = value;

            if (messageTmpText != null)
                messageTmpText.text = value;
        }
    }
}
