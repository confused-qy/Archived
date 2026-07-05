using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmployeeHandbook.Feishu
{
    public class FeishuContactChatController : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject chatPanelRoot;
        [SerializeField] private GameObject feishuWindowRoot;
        [SerializeField] private bool hidePanelWhenFeishuOpens = true;

        [Header("Message Roots")]
        [SerializeField] private Transform messageRoot;
        [SerializeField] private Transform choiceRoot;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private bool useScrollRectContentAsMessageRoot = true;
        [SerializeField] private bool overrideMessageSpacing = true;
        [SerializeField] private float messageSpacing = 6f;
        [SerializeField] private bool forceTopToBottomLayout = true;
        [SerializeField] private bool wrapMessagesInRows = true;
        [SerializeField] private float messageRowMinHeight = 32f;
        [SerializeField] private float messageRowHeightPadding = 2f;

        [Header("Date Divider")]
        [SerializeField] private bool showDateDividers = true;
        [SerializeField] private FeishuDateDividerView dateDividerPrefab;
        [SerializeField] private int startMonth = 3;
        [SerializeField] private float dateDividerRowHeight = 24f;

        [Header("Bubble Prefabs")]
        [SerializeField] private FeishuChatMessageView[] otherBubblePrefabs = new FeishuChatMessageView[5];
        [SerializeField] private FeishuChatMessageView[] playerBubblePrefabs = new FeishuChatMessageView[5];
        [SerializeField] private FeishuChoiceButton choiceButtonPrefab;

        [Header("Choices")]
        [SerializeField] private bool autoSetupChoiceRootLayout = true;
        [SerializeField] private float choiceSpacing = 6f;

        [Header("Header")]
        [SerializeField] private Text contactNameText;
        [SerializeField] private TMP_Text contactNameTmpText;

        [Header("Timing")]
        [SerializeField] private float defaultReplyDelay = 1f;
        [SerializeField] private bool autoFillUnlockedMessagesOnOpen = true;
        [SerializeField] private bool continueFillingAfterChoice = true;

        private FeishuConversationManager manager;
        private string contactName;
        private FeishuConversationData[] conversations;
        private FeishuConversationRuntimeState[] states;
        private int currentDay = 1;
        private int activeConversationIndex = -1;
        private Coroutine playRoutine;
        private Coroutine scrollRoutine;
        private readonly HashSet<int> renderedDateDays = new HashSet<int>();
        private bool hasInitialized;
        private bool isOpeningContact;

        private void Awake()
        {
            if (chatPanelRoot == null)
                chatPanelRoot = gameObject;

            EnsureMessageRoot();
            ApplyMessageSpacing();
            ApplyChoiceRootLayout();
            hasInitialized = true;
        }

        private void OnEnable()
        {
            if (!hasInitialized || !hidePanelWhenFeishuOpens || isOpeningContact)
                return;

            HidePanel();
        }

        public void OpenContact(
            string contact,
            FeishuConversationData[] contactConversations,
            FeishuConversationRuntimeState[] contactStates,
            FeishuConversationManager owner,
            int day)
        {
            StopPlaying();

            contactName = contact;
            conversations = contactConversations;
            states = contactStates;
            manager = owner;
            currentDay = Mathf.Max(1, day);
            activeConversationIndex = -1;

            isOpeningContact = true;
            if (chatPanelRoot != null)
                chatPanelRoot.SetActive(true);
            isOpeningContact = false;

            EnsureMessageRoot();
            ApplyMessageSpacing();
            ApplyChoiceRootLayout();
            SetContactName(contactName);
            ClearChildren(messageRoot);
            ClearChildren(choiceRoot);
            renderedDateDays.Clear();
            AutoFillPastUnlockedMessages();
            RenderAllTranscripts();
            ContinueFromFirstPendingConversation();
        }

        public void Choose(FeishuChoiceData choice)
        {
            if (choice == null || !HasActiveConversation())
                return;

            ClearChildren(choiceRoot);

            FeishuConversationRuntimeState state = states[activeConversationIndex];
            state.WaitingForChoice = false;

            AddTranscriptEntry(state, "player", choice.text, choice.bubbleSize);
            RenderMessage("player", choice.text, choice.bubbleSize);
            ExecuteUnlockClue(choice.unlockClueId);

            state.PendingNodeId = choice.next;
            if (continueFillingAfterChoice)
                ContinueFromActiveConversation();
        }

        public void HidePanel()
        {
            StopPlaying();
            ClearChildren(choiceRoot);

            if (chatPanelRoot != null)
                chatPanelRoot.SetActive(false);
        }

        public void CloseFeishuWindow()
        {
            FeishuSfxPlayer.PlayCloseClickSfx();
            HidePanel();

            if (feishuWindowRoot != null)
                feishuWindowRoot.SetActive(false);
        }

        private void ContinueFromFirstPendingConversation()
        {
            int pendingIndex = FindFirstPendingConversationIndex();
            if (pendingIndex < 0)
                return;

            activeConversationIndex = pendingIndex;

            if (states[pendingIndex].WaitingForChoice)
            {
                ShowChoiceNode(pendingIndex, states[pendingIndex].PendingNodeId);
                return;
            }

            ContinueFromActiveConversation();
        }

        private void ContinueFromActiveConversation()
        {
            if (!HasActiveConversation())
                return;

            if (playRoutine != null)
                StopCoroutine(playRoutine);

            playRoutine = StartCoroutine(PlayFromConversation(activeConversationIndex));
        }

        private void AutoFillPastUnlockedMessages()
        {
            if (!autoFillUnlockedMessagesOnOpen || conversations == null || states == null)
                return;

            for (int i = 0; i < conversations.Length && i < states.Length; i++)
            {
                if (conversations[i] == null || conversations[i].unlockDay >= currentDay)
                    continue;

                FeishuConversationRuntimeState state = states[i];
                if (state == null || state.Completed)
                    continue;

                AutoFillPastConversation(conversations[i], state);
            }
        }

        private void AutoFillPastConversation(FeishuConversationData conversation, FeishuConversationRuntimeState state)
        {
            string nodeId = state.PendingNodeId;
            while (!string.IsNullOrWhiteSpace(nodeId))
            {
                FeishuChatNode node = FindNode(conversation, nodeId);
                if (node == null)
                {
                    Debug.LogWarning("FeishuContactChatController 自动补齐时找不到节点：" + nodeId, this);
                    CompleteConversation(state);
                    return;
                }

                state.PendingNodeId = node.id;

                if (node.IsChoice)
                {
                    CompleteConversation(state);
                    return;
                }

                string speaker = node.IsPlayer ? "player" : "other";
                AddTranscriptEntry(state, speaker, node.text, node.bubbleSize);
                ExecuteUnlockClue(node.unlockClueId);

                nodeId = node.next;
                state.PendingNodeId = nodeId;
            }

            CompleteConversation(state);
        }

        private IEnumerator PlayFromConversation(int conversationIndex)
        {
            while (conversationIndex >= 0 && conversationIndex < conversations.Length)
            {
                activeConversationIndex = conversationIndex;
                FeishuConversationRuntimeState state = states[conversationIndex];
                string nodeId = state.PendingNodeId;

                while (!string.IsNullOrWhiteSpace(nodeId))
                {
                    FeishuChatNode node = FindNode(conversations[conversationIndex], nodeId);
                    if (node == null)
                    {
                        Debug.LogWarning("FeishuContactChatController 找不到节点：" + nodeId, this);
                        CompleteConversation(state);
                        break;
                    }

                    state.PendingNodeId = node.id;

                    if (node.IsChoice)
                    {
                        if (conversations[conversationIndex].unlockDay < currentDay)
                        {
                            CompleteConversation(state);
                            break;
                        }

                        state.WaitingForChoice = true;
                        ShowChoices(node);
                        playRoutine = null;
                        yield break;
                    }

                    float delay = node.delay > 0f ? node.delay : defaultReplyDelay;
                    if (ShouldDelayBeforeNode(conversationIndex, state))
                        yield return new WaitForSeconds(delay);

                    string speaker = node.IsPlayer ? "player" : "other";
                    if (state.Transcript.Count == 0)
                        RenderDateDividerIfNeeded(conversations[conversationIndex].unlockDay);

                    AddTranscriptEntry(state, speaker, node.text, node.bubbleSize);
                    RenderMessage(speaker, node.text, node.bubbleSize);
                    ExecuteUnlockClue(node.unlockClueId);

                    nodeId = node.next;
                    state.PendingNodeId = nodeId;
                }

                CompleteConversation(state);
                conversationIndex = FindFirstPendingConversationIndex();
            }

            activeConversationIndex = -1;
            playRoutine = null;
        }

        private bool ShouldDelayBeforeNode(int conversationIndex, FeishuConversationRuntimeState state)
        {
            if (state == null)
                return false;

            if (state.Transcript.Count > 0)
                return true;

            if (conversations != null
                && conversationIndex >= 0
                && conversationIndex < conversations.Length
                && conversations[conversationIndex] != null
                && conversations[conversationIndex].unlockDay == currentDay)
            {
                return false;
            }

            return HasRenderedPreviousConversation(conversationIndex);
        }

        private void ShowChoiceNode(int conversationIndex, string nodeId)
        {
            FeishuChatNode node = FindNode(conversations[conversationIndex], nodeId);
            if (node == null || !node.IsChoice)
                return;

            activeConversationIndex = conversationIndex;
            ShowChoices(node);
        }

        private void ShowChoices(FeishuChatNode node)
        {
            ClearChildren(choiceRoot);

            if (choiceRoot == null || choiceButtonPrefab == null || node.choices == null)
                return;

            ApplyChoiceRootLayout();

            for (int i = 0; i < node.choices.Length; i++)
            {
                FeishuChoiceButton button = Instantiate(choiceButtonPrefab, choiceRoot);
                button.Initialize(this, node.choices[i]);
            }
        }

        private void RenderAllTranscripts()
        {
            if (states == null)
                return;

            for (int i = 0; i < states.Length; i++)
            {
                FeishuConversationRuntimeState state = states[i];
                if (state == null)
                    continue;

                if (state.Transcript.Count > 0 && conversations != null && i < conversations.Length)
                    RenderDateDividerIfNeeded(conversations[i].unlockDay);

                for (int j = 0; j < state.Transcript.Count; j++)
                {
                    FeishuTranscriptEntry entry = state.Transcript[j];
                    RenderMessage(entry.Speaker, entry.Text, entry.BubbleSize);
                }
            }
        }

        private void AddTranscriptEntry(FeishuConversationRuntimeState state, string speaker, string text, int bubbleSize)
        {
            state.Transcript.Add(new FeishuTranscriptEntry
            {
                Speaker = speaker,
                Text = text,
                BubbleSize = bubbleSize
            });
        }

        private void RenderMessage(string speaker, string text, int bubbleSize)
        {
            if (messageRoot == null)
                return;

            FeishuChatMessageView prefab = GetBubblePrefab(speaker, bubbleSize);
            if (prefab == null)
            {
                Debug.LogWarning("FeishuContactChatController 缺少气泡 prefab，speaker=" + speaker + " bubbleSize=" + bubbleSize, this);
                return;
            }

            Transform parent = messageRoot;
            if (wrapMessagesInRows)
                parent = CreateMessageRow(speaker, prefab).transform;

            FeishuChatMessageView view = Instantiate(prefab, parent);
            NormalizeBubbleTransform(view.transform);
            view.SetText(text);
            RequestScrollToBottom();
        }

        private void RenderDateDividerIfNeeded(int day)
        {
            if (!showDateDividers || day <= 0 || renderedDateDays.Contains(day))
                return;

            if (messageRoot == null)
                return;

            renderedDateDays.Add(day);
            RenderDateDivider(FormatDate(day));
        }

        private void RenderDateDivider(string dateText)
        {
            if (dateDividerPrefab == null)
            {
                Debug.LogWarning("FeishuContactChatController 缺少 Date Divider Prefab，无法显示日期：" + dateText, this);
                return;
            }

            RectTransform row = CreateDateDividerRow();
            FeishuDateDividerView view = Instantiate(dateDividerPrefab, row);
            NormalizeBubbleTransform(view.transform);
            view.SetDateText(dateText);
            RequestScrollToBottom();
        }

        private string FormatDate(int day)
        {
            return startMonth + "/" + Mathf.Clamp(day, 1, 20);
        }

        private FeishuChatMessageView GetBubblePrefab(string speaker, int bubbleSize)
        {
            FeishuChatMessageView[] prefabs = string.Equals(speaker, "player", System.StringComparison.OrdinalIgnoreCase)
                ? playerBubblePrefabs
                : otherBubblePrefabs;

            if (prefabs == null || prefabs.Length == 0)
                return null;

            int index = Mathf.Clamp(bubbleSize, 1, prefabs.Length) - 1;
            return prefabs[index];
        }

        private int FindFirstPendingConversationIndex()
        {
            if (conversations == null || states == null)
                return -1;

            for (int i = 0; i < conversations.Length && i < states.Length; i++)
            {
                if (states[i] != null && !states[i].Completed)
                    return i;
            }

            return -1;
        }

        private bool HasActiveConversation()
        {
            return conversations != null
                && states != null
                && activeConversationIndex >= 0
                && activeConversationIndex < conversations.Length
                && activeConversationIndex < states.Length;
        }

        private bool HasRenderedPreviousConversation(int conversationIndex)
        {
            for (int i = 0; i < conversationIndex && i < states.Length; i++)
            {
                if (states[i] != null && states[i].Transcript.Count > 0)
                    return true;
            }

            return false;
        }

        private FeishuChatNode FindNode(FeishuConversationData conversation, string nodeId)
        {
            if (conversation == null || conversation.nodes == null)
                return null;

            for (int i = 0; i < conversation.nodes.Length; i++)
            {
                if (conversation.nodes[i] != null && conversation.nodes[i].id == nodeId)
                    return conversation.nodes[i];
            }

            return null;
        }

        private void ExecuteUnlockClue(int clueId)
        {
            if (clueId > 0 && manager != null)
                manager.UnlockClue(clueId);
        }

        public bool WillUnlockClue(int clueId)
        {
            return clueId > 0 && manager != null && manager.WillUnlockClue(clueId);
        }

        private void CompleteConversation(FeishuConversationRuntimeState state)
        {
            if (state == null)
                return;

            state.Completed = true;
            state.WaitingForChoice = false;
            state.PendingNodeId = string.Empty;
        }

        private void StopPlaying()
        {
            if (playRoutine != null)
            {
                StopCoroutine(playRoutine);
                playRoutine = null;
            }
        }

        private void SetContactName(string value)
        {
            if (contactNameText != null)
                contactNameText.text = value;

            if (contactNameTmpText != null)
                contactNameTmpText.text = value;
        }

        private void ScrollToBottom()
        {
            if (scrollRect == null)
                return;

            if (scrollRect.content != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);

            Canvas.ForceUpdateCanvases();
            scrollRect.velocity = Vector2.zero;
            scrollRect.verticalNormalizedPosition = 0f;
        }

        private void RequestScrollToBottom()
        {
            if (scrollRect == null)
                return;

            if (scrollRoutine != null)
                StopCoroutine(scrollRoutine);

            scrollRoutine = StartCoroutine(ScrollToBottomNextFrame());
        }

        private IEnumerator ScrollToBottomNextFrame()
        {
            yield return null;
            yield return new WaitForEndOfFrame();
            ScrollToBottom();
            yield return null;
            ScrollToBottom();
            scrollRoutine = null;
        }

        private void EnsureMessageRoot()
        {
            if (messageRoot != null)
                return;

            if (useScrollRectContentAsMessageRoot && scrollRect != null && scrollRect.content != null)
                messageRoot = scrollRect.content;
        }

        private void ApplyMessageSpacing()
        {
            if (!overrideMessageSpacing || messageRoot == null)
                return;

            VerticalLayoutGroup verticalLayoutGroup = messageRoot.GetComponent<VerticalLayoutGroup>();
            if (verticalLayoutGroup != null)
            {
                verticalLayoutGroup.spacing = messageSpacing;
                if (forceTopToBottomLayout)
                    verticalLayoutGroup.childAlignment = TextAnchor.UpperLeft;

                if (wrapMessagesInRows)
                {
                    verticalLayoutGroup.childControlHeight = true;
                    verticalLayoutGroup.childForceExpandHeight = false;
                    verticalLayoutGroup.childControlWidth = true;
                    verticalLayoutGroup.childForceExpandWidth = true;
                }
            }
        }

        private void ApplyChoiceRootLayout()
        {
            if (!autoSetupChoiceRootLayout || choiceRoot == null)
                return;

            VerticalLayoutGroup verticalLayoutGroup = choiceRoot.GetComponent<VerticalLayoutGroup>();
            if (verticalLayoutGroup == null)
                verticalLayoutGroup = choiceRoot.gameObject.AddComponent<VerticalLayoutGroup>();

            verticalLayoutGroup.spacing = choiceSpacing;
            verticalLayoutGroup.childAlignment = TextAnchor.UpperCenter;
            verticalLayoutGroup.childControlWidth = true;
            verticalLayoutGroup.childControlHeight = true;
            verticalLayoutGroup.childForceExpandWidth = false;
            verticalLayoutGroup.childForceExpandHeight = false;
        }

        private RectTransform CreateMessageRow(string speaker, FeishuChatMessageView prefab)
        {
            GameObject rowObject = new GameObject("MessageRow", typeof(RectTransform), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
            RectTransform row = rowObject.GetComponent<RectTransform>();
            row.SetParent(messageRoot, false);
            row.anchorMin = new Vector2(0f, 1f);
            row.anchorMax = new Vector2(1f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.sizeDelta = new Vector2(0f, GetMessageRowHeight(prefab));

            LayoutElement layoutElement = rowObject.GetComponent<LayoutElement>();
            layoutElement.minHeight = row.sizeDelta.y;
            layoutElement.preferredHeight = row.sizeDelta.y;

            HorizontalLayoutGroup horizontalLayoutGroup = rowObject.GetComponent<HorizontalLayoutGroup>();
            horizontalLayoutGroup.childAlignment = string.Equals(speaker, "player", System.StringComparison.OrdinalIgnoreCase)
                ? TextAnchor.MiddleRight
                : TextAnchor.MiddleLeft;
            horizontalLayoutGroup.childControlWidth = false;
            horizontalLayoutGroup.childControlHeight = false;
            horizontalLayoutGroup.childForceExpandWidth = false;
            horizontalLayoutGroup.childForceExpandHeight = false;
            horizontalLayoutGroup.padding = new RectOffset(0, 0, 0, 0);
            horizontalLayoutGroup.spacing = 0f;

            return row;
        }

        private RectTransform CreateDateDividerRow()
        {
            GameObject rowObject = new GameObject("DateDividerRow", typeof(RectTransform), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
            RectTransform row = rowObject.GetComponent<RectTransform>();
            row.SetParent(messageRoot, false);
            row.anchorMin = new Vector2(0f, 1f);
            row.anchorMax = new Vector2(1f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.sizeDelta = new Vector2(0f, dateDividerRowHeight);

            LayoutElement layoutElement = rowObject.GetComponent<LayoutElement>();
            layoutElement.minHeight = dateDividerRowHeight;
            layoutElement.preferredHeight = dateDividerRowHeight;

            HorizontalLayoutGroup horizontalLayoutGroup = rowObject.GetComponent<HorizontalLayoutGroup>();
            horizontalLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
            horizontalLayoutGroup.childControlWidth = false;
            horizontalLayoutGroup.childControlHeight = false;
            horizontalLayoutGroup.childForceExpandWidth = false;
            horizontalLayoutGroup.childForceExpandHeight = false;
            horizontalLayoutGroup.padding = new RectOffset(0, 0, 0, 0);
            horizontalLayoutGroup.spacing = 0f;

            return row;
        }

        private float GetMessageRowHeight(FeishuChatMessageView prefab)
        {
            RectTransform prefabRect = prefab != null ? prefab.GetComponent<RectTransform>() : null;
            float prefabHeight = prefabRect != null ? prefabRect.rect.height : 0f;
            if (prefabHeight <= 0f && prefabRect != null)
                prefabHeight = prefabRect.sizeDelta.y;

            return Mathf.Max(messageRowMinHeight, prefabHeight + messageRowHeightPadding);
        }

        private static void NormalizeBubbleTransform(Transform bubbleTransform)
        {
            RectTransform rectTransform = bubbleTransform as RectTransform;
            if (rectTransform == null)
                return;

            rectTransform.localScale = Vector3.one;
            rectTransform.anchoredPosition = Vector2.zero;
        }

        private static void ClearChildren(Transform root)
        {
            if (root == null)
                return;

            for (int i = root.childCount - 1; i >= 0; i--)
                Destroy(root.GetChild(i).gameObject);
        }
    }
}
