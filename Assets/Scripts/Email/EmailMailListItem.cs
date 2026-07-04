using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmployeeHandbook.Email
{
    public class EmailMailListItem : MonoBehaviour
    {
        [Header("State Objects")]
        [SerializeField] private GameObject selectedBackgroundObject;
        [SerializeField] private GameObject unreadDotObject;

        [Header("Button")]
        [SerializeField] private Button button;

        [Header("Texts")]
        [SerializeField] private Text senderText;
        [SerializeField] private TMP_Text senderTmpText;
        [SerializeField] private Text subjectText;
        [SerializeField] private TMP_Text subjectTmpText;
        [SerializeField] private Text dateText;
        [SerializeField] private TMP_Text dateTmpText;

        [Header("List Display")]
        [SerializeField] private int maxSubjectCharactersBeforeTruncate = 6;
        [SerializeField] private int truncatedSubjectCharacters = 5;
        [SerializeField] private int maxSenderCharactersBeforeTruncate = 4;
        [SerializeField] private int truncatedSenderCharacters = 3;

        public EmailMailData Mail { get; private set; }

        private EmailController controller;

        private void Awake()
        {
            if (button == null)
                button = GetComponentInChildren<Button>(true);
        }

        public void Initialize(EmailController owner, EmailMailData mail, bool opened, bool selected)
        {
            controller = owner;
            Mail = mail;

            SetText(senderText, senderTmpText, mail != null ? GetListSender(mail.sender) : string.Empty);
            SetText(subjectText, subjectTmpText, mail != null ? GetListSubject(mail.subject) : string.Empty);
            SetText(dateText, dateTmpText, mail != null ? mail.date : string.Empty);
            SetState(opened, selected);

            if (button == null)
                button = GetComponentInChildren<Button>(true);

            if (button != null)
            {
                button.onClick.RemoveListener(Select);
                button.onClick.AddListener(Select);
            }
        }

        public void SetState(bool opened, bool selected)
        {
            if (selectedBackgroundObject != null)
                selectedBackgroundObject.SetActive(selected);

            if (unreadDotObject != null)
                unreadDotObject.SetActive(!opened);
        }

        private void Select()
        {
            if (controller != null && Mail != null)
                controller.SelectMail(Mail);
        }

        private void SetText(Text text, TMP_Text tmpText, string value)
        {
            if (text != null)
                text.text = value;

            if (tmpText != null)
                tmpText.text = value;
        }

        private string GetListSubject(string subject)
        {
            return GetTruncatedText(subject, maxSubjectCharactersBeforeTruncate, truncatedSubjectCharacters);
        }

        private string GetListSender(string sender)
        {
            return GetTruncatedText(sender, maxSenderCharactersBeforeTruncate, truncatedSenderCharacters);
        }

        private string GetTruncatedText(string value, int maxCharactersBeforeTruncate, int truncatedCharacters)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            if (value.Length <= maxCharactersBeforeTruncate)
                return value;

            int length = Mathf.Clamp(truncatedCharacters, 0, value.Length);
            return value.Substring(0, length) + "...";
        }
    }
}
