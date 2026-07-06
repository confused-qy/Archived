using System;
using System.Collections.Generic;
using EmployeeHandbook.ClueSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DailyGameManager = EmployeeHandbook.DailyTasks.GameManager;

namespace EmployeeHandbook.Email
{
    public class EmailController : MonoBehaviour
    {
        [Header("Credentials")]
        [SerializeField] private string correctEmployeeId = "C017";
        [SerializeField] private string correctPassword = "monkeyai";

        [Header("Pages")]
        [SerializeField] private GameObject loginPage;
        [SerializeField] private GameObject inboxPage;
        [SerializeField] private GameObject composePopup;

        [Header("Inbox Data")]
        [SerializeField] private string mailsResourcePath = "email_mails";
        [SerializeField] private ClueNotebookClueList clueList;

        [Header("Mail List")]
        [SerializeField] private Transform mailListRoot;
        [SerializeField] private EmailMailListItem mailListItemPrefab;

        [Header("Mail Content")]
        [SerializeField] private GameObject mailContentPanel;
        [SerializeField] private Text contentSubjectText;
        [SerializeField] private TMP_Text contentSubjectTmpText;
        [SerializeField] private Text contentSenderText;
        [SerializeField] private TMP_Text contentSenderTmpText;
        [SerializeField] private Text contentBodyText;
        [SerializeField] private TMP_Text contentBodyTmpText;
        [SerializeField] private string emptyMailSubject = "未选择邮件";
        [SerializeField] private string emptyMailSender = "";
        [SerializeField] private string emptyMailBody = "";

        [Header("Login Inputs")]
        [SerializeField] private InputField employeeIdInput;
        [SerializeField] private TMP_InputField employeeIdTmpInput;
        [SerializeField] private InputField passwordInput;
        [SerializeField] private TMP_InputField passwordTmpInput;

        [Header("Auto Login")]
        [SerializeField] private Toggle autoLoginToggle;
        [SerializeField] private Button autoLoginButton;
        [SerializeField] private GameObject autoLoginCheckedObject;

        [Header("Buttons")]
        [SerializeField] private Button loginButton;
        [SerializeField] private Button writeMailButton;
        [SerializeField] private Button closeComposeButton;

        [Header("Optional Message")]
        [SerializeField] private Text messageText;
        [SerializeField] private TMP_Text messageTmpText;
        [SerializeField] private string wrongPasswordMessage = "工号或密码错误。";

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip loginSuccessClip;
        [SerializeField] private AudioClip loginFailureClip;
        [SerializeField] private AudioClip normalMailClickClip;
        [SerializeField] private AudioClip clueMailFirstClickClip;

        private bool autoLoginRemembered;
        private bool manualAutoLoginSelected;
        private EmailMailData[] mails;
        private readonly List<EmailMailListItem> spawnedMailItems = new List<EmailMailListItem>();
        private readonly HashSet<string> openedMailIds = new HashSet<string>();
        private string selectedMailId;
        private bool subscribedToGameManager;

        private void Awake()
        {
            LoadMails();
            SetupAudioSource();
            BindButtons();
            SetPasswordMode();
            UpdateAutoLoginVisual();
            ClearSelectedMail();
        }

        private void OnEnable()
        {
            SubscribeToGameManager();
            ResetToLoginPage();
        }

        private void OnDisable()
        {
            UnsubscribeFromGameManager();
        }

        public void ResetToLoginPage()
        {
            ShowLoginPage();
            HideComposePopup();
            SetMessage(string.Empty);

            if (autoLoginRemembered)
                FillCorrectCredentials();
            else
                ClearCredentials();

            UpdateAutoLoginVisual();
        }

        public void Login()
        {
            if (!IsCredentialCorrect())
            {
                SetMessage(wrongPasswordMessage);
                PlayOneShot(loginFailureClip);
                return;
            }

            if (IsAutoLoginSelected())
                autoLoginRemembered = true;

            SetMessage(string.Empty);
            PlayOneShot(loginSuccessClip);
            ShowInboxPage();
        }

        public void ToggleAutoLogin()
        {
            SetAutoLoginSelected(!IsAutoLoginSelected());

            if (autoLoginToggle != null)
                autoLoginToggle.SetIsOnWithoutNotify(manualAutoLoginSelected);
        }

        public void OpenComposePopup()
        {
            if (composePopup != null)
                composePopup.SetActive(true);
        }

        public void HideComposePopup()
        {
            if (composePopup != null)
                composePopup.SetActive(false);
        }

        private void ShowLoginPage()
        {
            if (loginPage != null)
                loginPage.SetActive(true);

            if (inboxPage != null)
                inboxPage.SetActive(false);
        }

        private void ShowInboxPage()
        {
            if (loginPage != null)
                loginPage.SetActive(false);

            if (inboxPage != null)
                inboxPage.SetActive(true);

            RefreshInbox();
            ClearSelectedMail();
        }

        public void RefreshInbox()
        {
            LoadMails();
            ClearMailList();

            if (mailListRoot == null)
            {
                Debug.LogWarning("EmailController 没有设置 Mail List Root。请拖：消息列表/Viewport/Content。", this);
                return;
            }

            if (mailListItemPrefab == null)
            {
                Debug.LogWarning("EmailController 没有设置 Mail List Item Prefab。请拖：邮件题目。", this);
                return;
            }

            if (mailListRoot == mailListItemPrefab.transform || mailListRoot.IsChildOf(mailListItemPrefab.transform))
            {
                Debug.LogWarning("EmailController 的 Mail List Root 拖错了。它必须是邮件题目的父物体 Content，不能是邮件题目自己或邮件题目的子物体。", this);
                return;
            }

            if (mails == null || mails.Length == 0)
            {
                Debug.LogWarning("EmailController 没有读到任何邮件。检查 Resources/" + mailsResourcePath + ".json 是否存在，并且根字段是否叫 mails。", this);
                return;
            }

            List<EmailMailData> unlockedMails = GetUnlockedMails();
            if (unlockedMails.Count == 0)
            {
                Debug.LogWarning("EmailController 当前第 " + GetCurrentDay() + " 天没有可显示邮件。检查 email_mails.json 里的 day 是否小于等于当前天。", this);
                return;
            }

            for (int i = 0; i < unlockedMails.Count; i++)
            {
                EmailMailData mail = unlockedMails[i];
                EmailMailListItem item = Instantiate(mailListItemPrefab, mailListRoot);
                ResetMailListItemTransform(item);
                item.gameObject.SetActive(true);
                item.Initialize(this, mail, IsMailOpened(mail), IsMailSelected(mail));
                spawnedMailItems.Add(item);
            }

            if (!string.IsNullOrEmpty(selectedMailId) && FindUnlockedMailById(selectedMailId) == null)
                ClearSelectedMail();

            Debug.Log("EmailController 已生成邮件数量：" + spawnedMailItems.Count + "，当前第 " + GetCurrentDay() + " 天。", this);
        }

        public void SelectMail(EmailMailData mail)
        {
            if (mail == null)
                return;

            PlayMailClickSound(mail);
            CompleteMailTaskIfNeeded(mail);

            selectedMailId = mail.mailId;

            if (!string.IsNullOrWhiteSpace(mail.mailId))
                openedMailIds.Add(mail.mailId);

            if (mailContentPanel != null)
                mailContentPanel.SetActive(true);

            SetContentText(mail.subject, mail.sender, mail.body);
            UpdateMailListItems();
            UnlockMailClueIfNeeded(mail);
        }

        public bool HasUnreadUnlockedMail()
        {
            if (mails == null || mails.Length == 0)
                LoadMails();

            List<EmailMailData> unlockedMails = GetUnlockedMails();
            for (int i = 0; i < unlockedMails.Count; i++)
            {
                EmailMailData mail = unlockedMails[i];
                if (mail != null && !IsMailOpened(mail))
                    return true;
            }

            return false;
        }

        private void BindButtons()
        {
            if (loginButton != null)
            {
                loginButton.onClick.RemoveListener(Login);
                loginButton.onClick.AddListener(Login);
            }

            if (autoLoginButton != null)
            {
                autoLoginButton.onClick.RemoveListener(ToggleAutoLogin);
                autoLoginButton.onClick.AddListener(ToggleAutoLogin);
            }

            if (autoLoginToggle != null)
            {
                autoLoginToggle.onValueChanged.RemoveListener(SetAutoLoginSelected);
                autoLoginToggle.onValueChanged.AddListener(SetAutoLoginSelected);
            }

            if (writeMailButton != null)
            {
                writeMailButton.onClick.RemoveListener(OpenComposePopup);
                writeMailButton.onClick.AddListener(OpenComposePopup);
            }

            if (closeComposeButton != null)
            {
                closeComposeButton.onClick.RemoveListener(HideComposePopup);
                closeComposeButton.onClick.AddListener(HideComposePopup);
            }
        }

        private void SetAutoLoginSelected(bool selected)
        {
            manualAutoLoginSelected = selected;

            if (!selected)
                autoLoginRemembered = false;

            UpdateAutoLoginVisual();
        }

        private bool IsAutoLoginSelected()
        {
            if (autoLoginToggle != null)
                return autoLoginToggle.isOn;

            return manualAutoLoginSelected;
        }

        private void UpdateAutoLoginVisual()
        {
            bool selected = IsAutoLoginSelected();

            if (autoLoginCheckedObject != null)
                autoLoginCheckedObject.SetActive(selected);
        }

        private bool IsCredentialCorrect()
        {
            return GetEmployeeId() == correctEmployeeId && GetPassword() == correctPassword;
        }

        private string GetEmployeeId()
        {
            if (employeeIdTmpInput != null)
                return employeeIdTmpInput.text.Trim();

            if (employeeIdInput != null)
                return employeeIdInput.text.Trim();

            return string.Empty;
        }

        private string GetPassword()
        {
            if (passwordTmpInput != null)
                return passwordTmpInput.text;

            if (passwordInput != null)
                return passwordInput.text;

            return string.Empty;
        }

        private void FillCorrectCredentials()
        {
            SetEmployeeId(correctEmployeeId);
            SetPassword(correctPassword);
        }

        private void ClearCredentials()
        {
            SetEmployeeId(string.Empty);
            SetPassword(string.Empty);
        }

        private void SetEmployeeId(string value)
        {
            if (employeeIdTmpInput != null)
                employeeIdTmpInput.text = value;

            if (employeeIdInput != null)
                employeeIdInput.text = value;
        }

        private void SetPassword(string value)
        {
            if (passwordTmpInput != null)
                passwordTmpInput.text = value;

            if (passwordInput != null)
                passwordInput.text = value;
        }

        private void SetPasswordMode()
        {
            if (passwordTmpInput != null)
            {
                passwordTmpInput.contentType = TMP_InputField.ContentType.Password;
                passwordTmpInput.ForceLabelUpdate();
            }

            if (passwordInput != null)
            {
                passwordInput.contentType = InputField.ContentType.Password;
                passwordInput.ForceLabelUpdate();
            }
        }

        private void SetMessage(string value)
        {
            if (messageText != null)
                messageText.text = value;

            if (messageTmpText != null)
                messageTmpText.text = value;
        }

        private void SetContentText(string subject, string sender, string body)
        {
            if (contentSubjectText != null)
                contentSubjectText.text = subject;

            if (contentSubjectTmpText != null)
                contentSubjectTmpText.text = subject;

            if (contentSenderText != null)
                contentSenderText.text = sender;

            if (contentSenderTmpText != null)
                contentSenderTmpText.text = sender;

            if (contentBodyText != null)
                contentBodyText.text = body;

            if (contentBodyTmpText != null)
                contentBodyTmpText.text = body;
        }

        private void ClearSelectedMail()
        {
            selectedMailId = null;
            SetContentText(emptyMailSubject, emptyMailSender, emptyMailBody);

            if (mailContentPanel != null)
                mailContentPanel.SetActive(false);

            UpdateMailListItems();
        }

        private void UpdateMailListItems()
        {
            for (int i = 0; i < spawnedMailItems.Count; i++)
            {
                EmailMailListItem item = spawnedMailItems[i];
                if (item == null)
                    continue;

                EmailMailData mail = item.Mail;
                item.SetState(IsMailOpened(mail), IsMailSelected(mail));
            }
        }

        private bool IsMailOpened(EmailMailData mail)
        {
            return mail != null && !string.IsNullOrWhiteSpace(mail.mailId) && openedMailIds.Contains(mail.mailId);
        }

        private bool IsMailSelected(EmailMailData mail)
        {
            return mail != null && !string.IsNullOrWhiteSpace(mail.mailId) && mail.mailId == selectedMailId;
        }

        private void UnlockMailClueIfNeeded(EmailMailData mail)
        {
            if (!CanUnlockMailClue(mail))
                return;

            clueList.UnlockClue(mail.unlockClueId);
        }

        private void PlayMailClickSound(EmailMailData mail)
        {
            if (CanUnlockMailClue(mail))
                return;

            PlayOneShot(normalMailClickClip);
        }

        private bool CanUnlockMailClue(EmailMailData mail)
        {
            return mail != null &&
                   mail.unlockClueId > 0 &&
                   clueList != null &&
                   clueList.WillUnlockClue(mail.unlockClueId);
        }

        private void CompleteMailTaskIfNeeded(EmailMailData mail)
        {
            if (mail == null || string.IsNullOrWhiteSpace(mail.completeTaskId))
                return;

            if (DailyGameManager.Instance == null)
            {
                Debug.LogWarning("邮件已阅读，但无法完成任务 " + mail.completeTaskId + "：场景中没有 DailyTasks.GameManager。", this);
                return;
            }

            DailyGameManager.Instance.ReportMiniGameSuccess(mail.completeTaskId);
        }

        private void ClearMailList()
        {
            for (int i = 0; i < spawnedMailItems.Count; i++)
            {
                if (spawnedMailItems[i] != null)
                    Destroy(spawnedMailItems[i].gameObject);
            }

            spawnedMailItems.Clear();
        }

        private void ResetMailListItemTransform(EmailMailListItem item)
        {
            if (item == null)
                return;

            RectTransform rectTransform = item.transform as RectTransform;
            if (rectTransform == null)
            {
                item.transform.localScale = Vector3.one;
                item.transform.localRotation = Quaternion.identity;
                item.transform.localPosition = Vector3.zero;
                return;
            }

            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.anchoredPosition3D = Vector3.zero;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private List<EmailMailData> GetUnlockedMails()
        {
            List<EmailMailData> unlockedMails = new List<EmailMailData>();
            int currentDay = GetCurrentDay();

            if (mails == null)
                return unlockedMails;

            for (int i = 0; i < mails.Length; i++)
            {
                EmailMailData mail = mails[i];
                if (mail != null && mail.day <= currentDay)
                    unlockedMails.Add(mail);
            }

            unlockedMails.Sort(CompareMailByNewestFirst);
            return unlockedMails;
        }

        private int CompareMailByNewestFirst(EmailMailData left, EmailMailData right)
        {
            if (left == null && right == null)
                return 0;

            if (left == null)
                return 1;

            if (right == null)
                return -1;

            int dayCompare = right.day.CompareTo(left.day);
            if (dayCompare != 0)
                return dayCompare;

            return string.CompareOrdinal(right.mailId, left.mailId);
        }

        private EmailMailData FindUnlockedMailById(string mailId)
        {
            if (mails == null || string.IsNullOrWhiteSpace(mailId))
                return null;

            int currentDay = GetCurrentDay();
            for (int i = 0; i < mails.Length; i++)
            {
                EmailMailData mail = mails[i];
                if (mail != null && mail.day <= currentDay && mail.mailId == mailId)
                    return mail;
            }

            return null;
        }

        private int GetCurrentDay()
        {
            if (DailyGameManager.Instance != null && DailyGameManager.Instance.CurrentState != null)
                return DailyGameManager.Instance.CurrentState.currentDay;

            return 1;
        }

        private void LoadMails()
        {
            mails = null;

            if (string.IsNullOrWhiteSpace(mailsResourcePath))
                return;

            TextAsset jsonAsset = Resources.Load<TextAsset>(mailsResourcePath);
            if (jsonAsset == null)
            {
                Debug.LogWarning("EmailController 找不到邮件配置：Resources/" + mailsResourcePath + ".json", this);
                return;
            }

            EmailMailCollection collection = JsonUtility.FromJson<EmailMailCollection>(jsonAsset.text);
            mails = collection != null ? collection.mails : null;
        }

        private void SubscribeToGameManager()
        {
            if (subscribedToGameManager || DailyGameManager.Instance == null)
                return;

            DailyGameManager.Instance.StateChanged += RefreshInbox;
            subscribedToGameManager = true;
        }

        private void UnsubscribeFromGameManager()
        {
            if (!subscribedToGameManager || DailyGameManager.Instance == null)
                return;

            DailyGameManager.Instance.StateChanged -= RefreshInbox;
            subscribedToGameManager = false;
        }

        private void SetupAudioSource()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.playOnAwake = false;
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (clip == null || audioSource == null || !audioSource.gameObject.activeInHierarchy)
                return;

            if (!audioSource.enabled)
                audioSource.enabled = true;

            audioSource.PlayOneShot(clip);
        }
    }

    [Serializable]
    public class EmailMailCollection
    {
        public EmailMailData[] mails;
    }

    [Serializable]
    public class EmailMailData
    {
        public string mailId;
        public int day;
        public string date;
        public string sender;
        public string subject;
        public int unlockClueId;
        public string completeTaskId;
        [TextArea(3, 10)] public string body;
    }
}
