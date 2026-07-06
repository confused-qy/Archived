using System.Collections.Generic;
using EmployeeHandbook.ClueSystem;
using UnityEngine;
using DailyGameManager = EmployeeHandbook.DailyTasks.GameManager;

namespace EmployeeHandbook.Feishu
{
    public class FeishuConversationManager : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private TextAsset conversationsJson;
        [SerializeField] private FeishuConversationData[] conversations;

        [Header("Scene References")]
        [SerializeField] private FeishuChatController chatController;
        [SerializeField] private FeishuContactChatController contactChatController;
        [SerializeField] private ClueNotebookClueList clueList;
        [SerializeField] private FeishuConversationButton[] conversationButtons;
        [SerializeField] private FeishuContactButton[] contactButtons;
        [SerializeField] private bool autoCollectConversationButtons = true;
        [SerializeField] private bool autoCollectContactButtons = true;

        [Header("Day")]
        [SerializeField] private int currentDay = 1;
        [SerializeField] private bool useGameManagerDay = true;
        [SerializeField] private bool listenToGameManagerStateChanged = true;

        private readonly Dictionary<string, FeishuConversationRuntimeState> states =
            new Dictionary<string, FeishuConversationRuntimeState>();
        private bool subscribedToGameManager;

        private void Awake()
        {
            LoadConversationData();

            if (autoCollectConversationButtons)
                conversationButtons = GetComponentsInChildren<FeishuConversationButton>(true);

            if (autoCollectContactButtons)
                contactButtons = GetComponentsInChildren<FeishuContactButton>(true);
        }

        private void OnEnable()
        {
            SubscribeToGameManager();
            RefreshFromGameManager();
        }

        private void Start()
        {
            SubscribeToGameManager();
            RefreshFromGameManager();
        }

        private void OnDisable()
        {
            UnsubscribeFromGameManager();
        }

        private void SubscribeToGameManager()
        {
            if (subscribedToGameManager || !listenToGameManagerStateChanged || DailyGameManager.Instance == null)
                return;

            DailyGameManager.Instance.StateChanged += RefreshFromGameManager;

            if (DailyGameManager.Instance.TaskManager != null)
                DailyGameManager.Instance.TaskManager.TasksChanged += RefreshFromGameManager;

            subscribedToGameManager = true;
        }

        private void UnsubscribeFromGameManager()
        {
            if (!subscribedToGameManager || DailyGameManager.Instance == null)
                return;

            DailyGameManager.Instance.StateChanged -= RefreshFromGameManager;

            if (DailyGameManager.Instance.TaskManager != null)
                DailyGameManager.Instance.TaskManager.TasksChanged -= RefreshFromGameManager;

            subscribedToGameManager = false;
        }

        public void LoadDay(int day)
        {
            currentDay = Mathf.Max(1, day);
            RefreshConversationButtons();
        }

        public void RefreshFromGameManager()
        {
            if (!useGameManagerDay || DailyGameManager.Instance == null || DailyGameManager.Instance.CurrentState == null)
            {
                RefreshConversationButtons();
                return;
            }

            LoadDay(DailyGameManager.Instance.CurrentState.currentDay);
        }

        public void OpenConversation(string conversationId)
        {
            FeishuConversationData conversation = FindConversation(conversationId);
            if (conversation == null)
            {
                Debug.LogWarning("FeishuConversationManager 找不到对话：" + conversationId, this);
                return;
            }

            if (!IsConversationUnlocked(conversation))
            {
                Debug.LogWarning("FeishuConversationManager 对话尚未解锁：" + conversationId, this);
                return;
            }

            if (chatController == null)
            {
                Debug.LogWarning("FeishuConversationManager 缺少 Chat Controller。", this);
                return;
            }

            chatController.OpenConversation(conversation, GetOrCreateState(conversation), this);
        }

        public bool IsConversationUnlocked(string conversationId)
        {
            FeishuConversationData conversation = FindConversation(conversationId);
            return conversation != null && IsConversationUnlocked(conversation);
        }

        public string GetContactName(string conversationId)
        {
            FeishuConversationData conversation = FindConversation(conversationId);
            return conversation != null ? conversation.contactName : string.Empty;
        }

        public void UnlockClue(int clueId)
        {
            if (clueId <= 0)
                return;

            EnsureClueList();

            if (clueList != null)
                clueList.UnlockClue(clueId);
        }

        public bool WillUnlockClue(int clueId)
        {
            EnsureClueList();

            return clueId > 0 && clueList != null && clueList.WillUnlockClue(clueId);
        }

        private void EnsureClueList()
        {
            if (clueList != null)
                return;

            ClueNotebookClueList[] clueLists = Resources.FindObjectsOfTypeAll<ClueNotebookClueList>();
            for (int i = 0; i < clueLists.Length; i++)
            {
                if (clueLists[i] != null && clueLists[i].gameObject.scene.IsValid())
                {
                    clueList = clueLists[i];
                    return;
                }
            }
        }

        public void RefreshConversationButtons()
        {
            if (conversationButtons == null)
            {
                RefreshContactButtons();
                return;
            }

            for (int i = 0; i < conversationButtons.Length; i++)
            {
                if (conversationButtons[i] != null)
                    conversationButtons[i].Refresh(this);
            }

            RefreshContactButtons();
        }

        public void OpenContact(string contactName)
        {
            RefreshFromGameManager();

            if (contactChatController == null)
            {
                Debug.LogWarning("FeishuConversationManager 缺少 Contact Chat Controller。", this);
                return;
            }

            FeishuConversationData[] contactConversations = GetUnlockedContactConversations(contactName);
            if (contactConversations == null || contactConversations.Length == 0)
            {
                Debug.LogWarning("FeishuConversationManager 当前没有已解锁联系人对话：" + contactName, this);
                return;
            }

            FeishuConversationRuntimeState[] contactStates =
                new FeishuConversationRuntimeState[contactConversations.Length];

            for (int i = 0; i < contactConversations.Length; i++)
            {
                contactStates[i] = GetOrCreateState(contactConversations[i]);
                contactStates[i].LastReadDay = currentDay;
                contactStates[i].HasBeenOpened = true;
            }

            contactChatController.OpenContact(contactName, contactConversations, contactStates, this, currentDay);
            RefreshContactButtons();
        }

        public bool IsContactUnlocked(string contactName)
        {
            if (conversations == null || string.IsNullOrWhiteSpace(contactName))
                return false;

            for (int i = 0; i < conversations.Length; i++)
            {
                if (conversations[i] != null
                    && conversations[i].contactName == contactName
                    && IsConversationUnlocked(conversations[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasUnread(string contactName)
        {
            RefreshDayOnly();

            if (conversations == null || string.IsNullOrWhiteSpace(contactName))
                return false;

            for (int i = 0; i < conversations.Length; i++)
            {
                FeishuConversationData conversation = conversations[i];
                if (conversation == null || conversation.contactName != contactName)
                    continue;

                if (!IsConversationUnlocked(conversation))
                    continue;

                FeishuConversationRuntimeState state = GetOrCreateState(conversation);
                if (!state.HasBeenOpened)
                    return true;
            }

            return false;
        }

        public bool HasAnyUnread()
        {
            RefreshDayOnly();

            if (conversations == null || conversations.Length == 0)
                LoadConversationData();

            if (conversations == null)
                return false;

            for (int i = 0; i < conversations.Length; i++)
            {
                FeishuConversationData conversation = conversations[i];
                if (conversation == null || !IsConversationUnlocked(conversation))
                    continue;

                FeishuConversationRuntimeState state = GetOrCreateState(conversation);
                if (!state.HasBeenOpened)
                    return true;
            }

            return false;
        }

        public void RefreshContactButtons()
        {
            if (contactButtons == null)
                return;

            for (int i = 0; i < contactButtons.Length; i++)
            {
                if (contactButtons[i] != null)
                    contactButtons[i].Refresh(this);
            }
        }

        private void LoadConversationData()
        {
            if (conversationsJson == null)
                return;

            FeishuConversationCollection collection =
                JsonUtility.FromJson<FeishuConversationCollection>(conversationsJson.text);

            if (collection == null || collection.conversations == null)
            {
                Debug.LogWarning("FeishuConversationManager 读取 conversationsJson 失败。", this);
                return;
            }

            conversations = collection.conversations;
        }

        private void RefreshDayOnly()
        {
            if (!useGameManagerDay || DailyGameManager.Instance == null || DailyGameManager.Instance.CurrentState == null)
                return;

            currentDay = Mathf.Max(1, DailyGameManager.Instance.CurrentState.currentDay);
        }

        private FeishuConversationData FindConversation(string conversationId)
        {
            if (conversations == null || string.IsNullOrWhiteSpace(conversationId))
                return null;

            for (int i = 0; i < conversations.Length; i++)
            {
                if (conversations[i] != null && conversations[i].conversationId == conversationId)
                    return conversations[i];
            }

            return null;
        }

        private bool IsConversationUnlocked(FeishuConversationData conversation)
        {
            return conversation != null && conversation.unlockDay <= currentDay;
        }

        private FeishuConversationData[] GetUnlockedContactConversations(string contactName)
        {
            if (conversations == null || string.IsNullOrWhiteSpace(contactName))
                return new FeishuConversationData[0];

            List<FeishuConversationData> result = new List<FeishuConversationData>();
            for (int i = 0; i < conversations.Length; i++)
            {
                if (conversations[i] != null
                    && conversations[i].contactName == contactName
                    && IsConversationUnlocked(conversations[i]))
                {
                    result.Add(conversations[i]);
                }
            }

            return result.ToArray();
        }

        private FeishuConversationRuntimeState GetOrCreateState(FeishuConversationData conversation)
        {
            FeishuConversationRuntimeState state;
            if (!states.TryGetValue(conversation.conversationId, out state))
            {
                state = new FeishuConversationRuntimeState();
                state.PendingNodeId = GetFirstNodeId(conversation);
                states.Add(conversation.conversationId, state);
            }

            return state;
        }

        private static string GetFirstNodeId(FeishuConversationData conversation)
        {
            if (conversation == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(conversation.firstNodeId))
                return conversation.firstNodeId;

            if (conversation.nodes != null && conversation.nodes.Length > 0 && conversation.nodes[0] != null)
                return conversation.nodes[0].id;

            return string.Empty;
        }
    }
}
