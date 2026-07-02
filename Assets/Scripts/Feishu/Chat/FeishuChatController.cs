using System.Collections;
using EmployeeHandbook.ClueSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmployeeHandbook.Feishu
{
    public class FeishuChatController : MonoBehaviour
    {
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

        [Header("Optional")]
        [SerializeField] private ClueNotebookClueList clueList;
        [SerializeField] private bool continuePendingConversationOnOpen = true;
        [SerializeField] private float defaultReplyDelay = 1f;

        private FeishuConversationData conversation;
        private FeishuConversationRuntimeState state;
        private FeishuConversationManager manager;
        private Coroutine playRoutine;
        private Coroutine scrollRoutine;

        private void Awake()
        {
            EnsureMessageRoot();
            ApplyMessageSpacing();
            ApplyChoiceRootLayout();
        }

        public void OpenConversation(
            FeishuConversationData conversationData,
            FeishuConversationRuntimeState runtimeState,
            FeishuConversationManager owner)
        {
            StopPlaying();

            conversation = conversationData;
            state = runtimeState;
            manager = owner;

            if (conversation == null || state == null)
                return;

            EnsureMessageRoot();
            ApplyMessageSpacing();
            ApplyChoiceRootLayout();
            SetContactName(conversation.contactName);
            ClearChildren(messageRoot);
            ClearChildren(choiceRoot);
            RenderTranscript();

            if (state.Completed)
                return;

            if (state.WaitingForChoice)
                ShowChoiceNode(state.PendingNodeId);
            else if (continuePendingConversationOnOpen)
                ContinueFromPendingNode();
        }

        public void Choose(FeishuChoiceData choice)
        {
            if (choice == null || conversation == null || state == null)
                return;

            ClearChildren(choiceRoot);
            state.WaitingForChoice = false;

            AddTranscriptEntry("player", choice.text, choice.bubbleSize);
            RenderMessage("player", choice.text, choice.bubbleSize);
            ExecuteUnlockClue(choice.unlockClueId);

            state.PendingNodeId = choice.next;
            ContinueFromPendingNode();
        }

        private void ContinueFromPendingNode()
        {
            if (playRoutine != null)
                StopCoroutine(playRoutine);

            playRoutine = StartCoroutine(PlayFromNode(state.PendingNodeId));
        }

        private IEnumerator PlayFromNode(string nodeId)
        {
            while (!string.IsNullOrWhiteSpace(nodeId))
            {
                FeishuChatNode node = FindNode(nodeId);
                if (node == null)
                {
                    Debug.LogWarning("FeishuChatController 找不到节点：" + nodeId, this);
                    CompleteConversation();
                    yield break;
                }

                state.PendingNodeId = node.id;

                if (node.IsChoice)
                {
                    state.WaitingForChoice = true;
                    ShowChoices(node);
                    playRoutine = null;
                    yield break;
                }

                float delay = node.delay > 0f ? node.delay : defaultReplyDelay;
                if (state.Transcript.Count > 0)
                    yield return new WaitForSeconds(delay);

                string speaker = node.IsPlayer ? "player" : "other";
                AddTranscriptEntry(speaker, node.text, node.bubbleSize);
                RenderMessage(speaker, node.text, node.bubbleSize);
                ExecuteUnlockClue(node.unlockClueId);

                nodeId = node.next;
                state.PendingNodeId = nodeId;
            }

            CompleteConversation();
            playRoutine = null;
        }

        private void ShowChoiceNode(string nodeId)
        {
            FeishuChatNode node = FindNode(nodeId);
            if (node == null || !node.IsChoice)
                return;

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

        private void AddTranscriptEntry(string speaker, string text, int bubbleSize)
        {
            state.Transcript.Add(new FeishuTranscriptEntry
            {
                Speaker = speaker,
                Text = text,
                BubbleSize = bubbleSize
            });
        }

        private void RenderTranscript()
        {
            if (state == null)
                return;

            for (int i = 0; i < state.Transcript.Count; i++)
            {
                FeishuTranscriptEntry entry = state.Transcript[i];
                RenderMessage(entry.Speaker, entry.Text, entry.BubbleSize);
            }
        }

        private void RenderMessage(string speaker, string text, int bubbleSize)
        {
            if (messageRoot == null)
                return;

            FeishuChatMessageView prefab = GetBubblePrefab(speaker, bubbleSize);
            if (prefab == null)
            {
                Debug.LogWarning("FeishuChatController 缺少气泡 prefab，speaker=" + speaker + " bubbleSize=" + bubbleSize, this);
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

        private FeishuChatNode FindNode(string nodeId)
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
            if (clueId <= 0)
                return;

            if (manager != null)
            {
                manager.UnlockClue(clueId);
                return;
            }

            if (clueList != null)
                clueList.UnlockClue(clueId);
        }

        private void CompleteConversation()
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
