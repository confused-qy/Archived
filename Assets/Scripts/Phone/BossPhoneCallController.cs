using System;
using System.Collections;
using System.Collections.Generic;
using EmployeeHandbook.ClueSystem;
using EmployeeHandbook.Feishu;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DailyGameManager = EmployeeHandbook.DailyTasks.GameManager;

namespace EmployeeHandbook.Phone
{
    public class BossPhoneCallController : MonoBehaviour
    {
        private enum PhoneState
        {
            Idle,
            WaitingToRing,
            Ringing,
            IncomingOpen,
            InCall,
            Finished
        }

        [Header("Data")]
        [SerializeField] private string callsResourcePath = "boss_phone_calls";

        [Header("Phone")]
        [SerializeField] private Button phoneButton;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip ringClip;
        [SerializeField] private AudioClip answerClip;
        [SerializeField] private AudioClip dialogueClickClip;
        [SerializeField] private AudioClip hangupClip;
        [SerializeField] private float defaultRingDelay = 2f;

        [Header("Incoming Call")]
        [SerializeField] private GameObject incomingCallScreen;
        [SerializeField] private Slider answerSlider;
        [SerializeField] private float answerThreshold = 0.9f;

        [Header("Dialogue")]
        [SerializeField] private GameObject dialogueScreen;
        [SerializeField] private GameObject dialogueContentRoot;
        [SerializeField] private GameObject bossPortrait;
        [SerializeField] private Text speakerNameText;
        [SerializeField] private TMP_Text speakerNameTmpText;
        [SerializeField] private string bossName = "Boss";
        [SerializeField] private Text dialogueText;
        [SerializeField] private TMP_Text dialogueTmpText;
        [SerializeField] private Button textBoxButton;
        [SerializeField] private Transform choiceRoot;
        [SerializeField] private BossPhoneChoiceButton choiceButtonPrefab;
        [SerializeField] private float hangupScreenDelay = 1f;

        [Header("Typewriter")]
        [SerializeField] private bool useTypewriter = true;
        [SerializeField] private float charactersPerSecond = 12f;

        [Header("Optional")]
        [SerializeField] private ClueNotebookClueList clueList;
        [SerializeField] private bool listenToGameManager = true;
        [SerializeField] private bool autoStartOnEnable = true;

        [Header("Fade")]
        [SerializeField] private bool useFadeTransitions = true;
        [SerializeField] private float fadeDuration = 0.25f;

        private BossPhoneCallData[] calls;
        private BossPhoneCallData currentCall;
        private BossPhoneNodeData currentNode;
        private PhoneState state = PhoneState.Idle;
        private Coroutine ringDelayRoutine;
        private Coroutine finishRoutine;
        private Coroutine typewriterRoutine;
        private readonly Dictionary<GameObject, Coroutine> fadeRoutines = new Dictionary<GameObject, Coroutine>();
        private bool subscribedToGameManager;
        private bool waitingForFinalClick;
        private bool dialogueContentVisible;
        private bool isTypingDialogue;
        private string currentFullDialogueText = string.Empty;
        private readonly System.Collections.Generic.HashSet<string> finishedCallIds =
            new System.Collections.Generic.HashSet<string>();

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            if (audioSource != null)
            {
                audioSource.enabled = true;
                audioSource.playOnAwake = false;
            }

            if (dialogueContentRoot == null && textBoxButton != null && textBoxButton.transform.parent != null)
                dialogueContentRoot = textBoxButton.transform.parent.gameObject;

            LoadCalls();
            BindUiEvents();
            HideAllScreens();
        }

        private void OnEnable()
        {
            SubscribeToGameManager();

            if (autoStartOnEnable)
                RefreshForCurrentDay();
        }

        private void Start()
        {
            SubscribeToGameManager();
            RefreshForCurrentDay();
        }

        private void OnDisable()
        {
            UnsubscribeFromGameManager();
            StopRingDelay();
            StopFinishRoutine();
            StopTypewriter();
            StopAllFadeRoutines();
            StopRingSound();
        }

        public void RefreshForCurrentDay()
        {
            int day = GetCurrentDay();
            currentCall = FindCallForDay(day);
            currentNode = null;
            waitingForFinalClick = false;
            dialogueContentVisible = false;
            HideAllScreens();
            StopRingDelay();
            StopFinishRoutine();
            StopTypewriter();
            StopRingSound();

            if (currentCall == null || IsCallFinished(currentCall))
            {
                state = currentCall != null ? PhoneState.Finished : PhoneState.Idle;
                return;
            }

            state = PhoneState.WaitingToRing;
            float delay = currentCall.ringDelay > 0f ? currentCall.ringDelay : defaultRingDelay;
            ringDelayRoutine = StartCoroutine(StartRingingAfterDelay(delay));
        }

        public void OpenIncomingCallScreen()
        {
            if (state != PhoneState.Ringing && state != PhoneState.IncomingOpen)
                return;

            state = PhoneState.IncomingOpen;

            ShowWithFade(incomingCallScreen);
            HideWithFade(dialogueScreen);

            if (answerSlider != null)
                answerSlider.value = 0f;
        }

        public void OnTextBoxClicked()
        {
            if (state != PhoneState.InCall)
                return;

            PlayDialogueClickSound();

            if (isTypingDialogue)
            {
                CompleteTypewriter();
                return;
            }

            if (waitingForFinalClick)
            {
                BeginFinishCall();
                return;
            }

            if (currentNode == null || HasChoices(currentNode))
                return;

            if (string.IsNullOrWhiteSpace(currentNode.next))
            {
                BeginFinishCall();
                return;
            }

            GoToNode(currentNode.next);
        }

        public void Choose(BossPhoneChoiceData choice)
        {
            if (state != PhoneState.InCall || choice == null)
                return;

            UnlockClue(choice.unlockClueId);
            ClearChoices();
            GoToNode(choice.next);
        }

        public void PlayDialogueClickSound()
        {
            if (PlayOneShot(dialogueClickClip))
                return;

            FeishuSfxPlayer.PlaySendMessageSfx();
        }

        private IEnumerator StartRingingAfterDelay(float delay)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            ringDelayRoutine = null;

            if (state != PhoneState.WaitingToRing || currentCall == null || IsCallFinished(currentCall))
                yield break;

            state = PhoneState.Ringing;
            PlayRingSound();
        }

        private void AnswerCall()
        {
            if (state != PhoneState.IncomingOpen)
                return;

            StopRingSound();
            PlayAnswerSound();

            HideWithFade(incomingCallScreen);
            ShowWithFade(dialogueScreen);

            ShowDialogueContent();

            if (bossPortrait != null)
                bossPortrait.SetActive(true);

            SetSpeakerName(bossName);
            ClearChoices();
            state = PhoneState.InCall;
            GoToNode(GetFirstNodeId(currentCall));
        }

        private void GoToNode(string nodeId)
        {
            waitingForFinalClick = false;

            if (string.IsNullOrWhiteSpace(nodeId))
            {
                waitingForFinalClick = true;
                return;
            }

            BossPhoneNodeData node = FindNode(currentCall, nodeId);
            if (node == null)
            {
                Debug.LogWarning("BossPhoneCallController 找不到电话节点：" + nodeId, this);
                waitingForFinalClick = true;
                return;
            }

            currentNode = node;
            ShowDialogueContentIfNeeded();
            StartDialogueText(node.text);
            UnlockClue(node.unlockClueId);
            ClearChoices();
        }

        private void BeginFinishCall()
        {
            if (finishRoutine != null)
                return;

            finishRoutine = StartCoroutine(FinishCallAfterDelay());
        }

        private IEnumerator FinishCallAfterDelay()
        {
            if (currentCall != null && !string.IsNullOrWhiteSpace(currentCall.callId))
                finishedCallIds.Add(currentCall.callId);

            state = PhoneState.Finished;
            waitingForFinalClick = false;
            dialogueContentVisible = false;
            ClearChoices();
            ClearDialogueText();
            SetSpeakerName(string.Empty);
            HideDialogueContent();
            StopRingSound();

            if (hangupScreenDelay > 0f)
                yield return new WaitForSeconds(hangupScreenDelay);

            PlayHangupSound();
            HideWithFade(dialogueScreen);
            if (answerSlider != null)
                answerSlider.value = 0f;

            finishRoutine = null;
        }

        private void FinishCallImmediately()
        {
            if (currentCall != null && !string.IsNullOrWhiteSpace(currentCall.callId))
                finishedCallIds.Add(currentCall.callId);

            StopFinishRoutine();
            ClearChoices();
            ClearDialogueText();
            SetSpeakerName(string.Empty);
            HideDialogueContent();
            PlayHangupSound();
            SetVisibleImmediately(incomingCallScreen, false);
            SetVisibleImmediately(dialogueScreen, false);
            StopRingSound();
            state = PhoneState.Finished;
        }

        private void PlayHangupSound()
        {
            PlayOneShot(hangupClip);
        }

        private void PlayAnswerSound()
        {
            PlayOneShot(answerClip);
        }

        private bool PlayOneShot(AudioClip clip)
        {
            if (clip == null || audioSource == null || !audioSource.gameObject.activeInHierarchy)
                return false;

            if (!audioSource.enabled)
                audioSource.enabled = true;

            audioSource.PlayOneShot(clip);
            return true;
        }

        private void ShowDialogueContent()
        {
            if (dialogueContentRoot != null)
            {
                if (bossPortrait != null)
                    bossPortrait.SetActive(true);

                if (textBoxButton != null)
                    textBoxButton.gameObject.SetActive(true);

                ShowWithFade(dialogueContentRoot);
                dialogueContentVisible = true;
                return;
            }

            ShowWithFade(bossPortrait);
            ShowWithFade(textBoxButton != null ? textBoxButton.gameObject : null);
            dialogueContentVisible = true;
        }

        private void ShowDialogueContentIfNeeded()
        {
            if (dialogueContentVisible)
                return;

            ShowDialogueContent();
        }

        private void HideDialogueContent()
        {
            dialogueContentVisible = false;

            if (dialogueContentRoot != null)
            {
                HideWithFade(dialogueContentRoot);
                return;
            }

            HideWithFade(bossPortrait);
            HideWithFade(textBoxButton != null ? textBoxButton.gameObject : null);
        }

        private void BindUiEvents()
        {
            if (phoneButton != null)
            {
                phoneButton.onClick.RemoveListener(OpenIncomingCallScreen);
                phoneButton.onClick.AddListener(OpenIncomingCallScreen);
            }

            if (textBoxButton != null)
            {
                textBoxButton.onClick.RemoveListener(OnTextBoxClicked);
                textBoxButton.onClick.AddListener(OnTextBoxClicked);
            }

            if (answerSlider != null)
            {
                answerSlider.onValueChanged.RemoveListener(HandleAnswerSliderChanged);
                answerSlider.onValueChanged.AddListener(HandleAnswerSliderChanged);
            }
        }

        private void HandleAnswerSliderChanged(float value)
        {
            if (state == PhoneState.IncomingOpen && value >= answerThreshold)
                AnswerCall();
        }

        private void ShowChoices(BossPhoneNodeData node)
        {
            ClearChoices();

            if (choiceRoot == null || choiceButtonPrefab == null || node == null || node.choices == null)
                return;

            choiceRoot.gameObject.SetActive(true);

            for (int i = 0; i < node.choices.Length; i++)
            {
                BossPhoneChoiceButton button = Instantiate(choiceButtonPrefab, choiceRoot);
                button.Initialize(this, node.choices[i]);
            }
        }

        private void ClearChoices()
        {
            if (choiceRoot == null)
                return;

            for (int i = choiceRoot.childCount - 1; i >= 0; i--)
                Destroy(choiceRoot.GetChild(i).gameObject);

            choiceRoot.gameObject.SetActive(false);
        }

        private bool HasChoices(BossPhoneNodeData node)
        {
            return node != null && node.choices != null && node.choices.Length > 0;
        }

        private void HideAllScreens()
        {
            StopAllFadeRoutines();
            SetVisibleImmediately(incomingCallScreen, false);
            SetVisibleImmediately(dialogueScreen, false);
            dialogueContentVisible = false;

            SetVisibleImmediately(dialogueContentRoot, false);
            if (dialogueContentRoot == null)
            {
                SetVisibleImmediately(bossPortrait, false);
                SetVisibleImmediately(textBoxButton != null ? textBoxButton.gameObject : null, false);
            }

            if (answerSlider != null)
                answerSlider.value = 0f;

            ClearChoices();
        }

        private void ShowWithFade(GameObject target)
        {
            if (target == null)
                return;

            if (!useFadeTransitions || fadeDuration <= 0f)
            {
                SetVisibleImmediately(target, true);
                return;
            }

            StartFade(target, true);
        }

        private void HideWithFade(GameObject target)
        {
            if (target == null)
                return;

            if (!target.activeSelf)
                return;

            if (!useFadeTransitions || fadeDuration <= 0f)
            {
                SetVisibleImmediately(target, false);
                return;
            }

            StartFade(target, false);
        }

        private void StartFade(GameObject target, bool show)
        {
            StopFadeRoutine(target);
            fadeRoutines[target] = StartCoroutine(FadeObject(target, show));
        }

        private IEnumerator FadeObject(GameObject target, bool show)
        {
            CanvasGroup canvasGroup = EnsureCanvasGroup(target);
            if (canvasGroup == null)
            {
                target.SetActive(show);
                yield break;
            }

            if (show)
                target.SetActive(true);

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            float from = show ? 0f : canvasGroup.alpha;
            float to = show ? 1f : 0f;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                canvasGroup.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }

            canvasGroup.alpha = to;
            canvasGroup.interactable = show;
            canvasGroup.blocksRaycasts = show;

            if (!show)
                target.SetActive(false);

            fadeRoutines.Remove(target);
        }

        private void SetVisibleImmediately(GameObject target, bool visible)
        {
            if (target == null)
                return;

            StopFadeRoutine(target);

            CanvasGroup canvasGroup = EnsureCanvasGroup(target);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
            }

            target.SetActive(visible);
        }

        private CanvasGroup EnsureCanvasGroup(GameObject target)
        {
            if (target == null)
                return null;

            CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = target.AddComponent<CanvasGroup>();

            return canvasGroup;
        }

        private void StopFadeRoutine(GameObject target)
        {
            if (target == null)
                return;

            Coroutine routine;
            if (!fadeRoutines.TryGetValue(target, out routine))
                return;

            if (routine != null)
                StopCoroutine(routine);

            fadeRoutines.Remove(target);
        }

        private void StopAllFadeRoutines()
        {
            foreach (Coroutine routine in fadeRoutines.Values)
            {
                if (routine != null)
                    StopCoroutine(routine);
            }

            fadeRoutines.Clear();
        }

        private void PlayRingSound()
        {
            if (audioSource == null || ringClip == null || !audioSource.gameObject.activeInHierarchy)
                return;

            if (!audioSource.enabled)
                audioSource.enabled = true;

            audioSource.clip = ringClip;
            audioSource.loop = true;
            audioSource.Play();
        }

        private void StopRingSound()
        {
            if (audioSource == null)
                return;

            if (audioSource.clip == ringClip)
            {
                audioSource.Stop();
                audioSource.clip = null;
            }

            audioSource.loop = false;
        }

        private void StopRingDelay()
        {
            if (ringDelayRoutine == null)
                return;

            StopCoroutine(ringDelayRoutine);
            ringDelayRoutine = null;
        }

        private void StopFinishRoutine()
        {
            if (finishRoutine == null)
                return;

            StopCoroutine(finishRoutine);
            finishRoutine = null;
        }

        private void SetSpeakerName(string value)
        {
            if (speakerNameText != null)
                speakerNameText.text = value;

            if (speakerNameTmpText != null)
                speakerNameTmpText.text = value;
        }

        private void SetDialogueText(string value)
        {
            if (dialogueText != null)
                dialogueText.text = value;

            if (dialogueTmpText != null)
                dialogueTmpText.text = value;
        }

        private void StartDialogueText(string value)
        {
            StopTypewriter();
            currentFullDialogueText = value ?? string.Empty;

            if (!useTypewriter || charactersPerSecond <= 0f || string.IsNullOrEmpty(currentFullDialogueText))
            {
                SetDialogueText(currentFullDialogueText);
                FinishDialogueTextReveal();
                return;
            }

            SetDialogueText(string.Empty);
            isTypingDialogue = true;
            typewriterRoutine = StartCoroutine(TypeDialogueText(currentFullDialogueText));
        }

        private IEnumerator TypeDialogueText(string value)
        {
            float delay = 1f / charactersPerSecond;

            for (int i = 1; i <= value.Length; i++)
            {
                SetDialogueText(value.Substring(0, i));
                yield return new WaitForSeconds(delay);
            }

            typewriterRoutine = null;
            FinishDialogueTextReveal();
        }

        private void CompleteTypewriter()
        {
            StopTypewriter();
            SetDialogueText(currentFullDialogueText);
            FinishDialogueTextReveal();
        }

        private void FinishDialogueTextReveal()
        {
            isTypingDialogue = false;

            if (currentNode != null && HasChoices(currentNode))
                ShowChoices(currentNode);
        }

        private void ClearDialogueText()
        {
            StopTypewriter();
            currentFullDialogueText = string.Empty;
            SetDialogueText(string.Empty);
        }

        private void StopTypewriter()
        {
            if (typewriterRoutine != null)
            {
                StopCoroutine(typewriterRoutine);
                typewriterRoutine = null;
            }

            isTypingDialogue = false;
        }

        private void UnlockClue(int clueId)
        {
            if (clueId > 0 && clueList != null)
                clueList.UnlockClue(clueId);
        }

        private void LoadCalls()
        {
            calls = null;

            if (string.IsNullOrEmpty(callsResourcePath))
                return;

            TextAsset jsonAsset = Resources.Load<TextAsset>(callsResourcePath);
            if (jsonAsset == null)
            {
                Debug.LogWarning("BossPhoneCallController 找不到电话配置：Resources/" + callsResourcePath + ".json", this);
                return;
            }

            BossPhoneCallCollection collection =
                JsonUtility.FromJson<BossPhoneCallCollection>(jsonAsset.text);
            calls = collection != null ? collection.calls : null;
        }

        private BossPhoneCallData FindCallForDay(int day)
        {
            if (calls == null)
                return null;

            for (int i = 0; i < calls.Length; i++)
            {
                if (calls[i] != null && calls[i].day == day)
                    return calls[i];
            }

            return null;
        }

        private BossPhoneNodeData FindNode(BossPhoneCallData call, string nodeId)
        {
            if (call == null || call.nodes == null || string.IsNullOrWhiteSpace(nodeId))
                return null;

            for (int i = 0; i < call.nodes.Length; i++)
            {
                if (call.nodes[i] != null && call.nodes[i].id == nodeId)
                    return call.nodes[i];
            }

            return null;
        }

        private string GetFirstNodeId(BossPhoneCallData call)
        {
            if (call == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(call.firstNodeId))
                return call.firstNodeId;

            if (call.nodes != null && call.nodes.Length > 0 && call.nodes[0] != null)
                return call.nodes[0].id;

            return string.Empty;
        }

        private bool IsCallFinished(BossPhoneCallData call)
        {
            return call != null && !string.IsNullOrWhiteSpace(call.callId) && finishedCallIds.Contains(call.callId);
        }

        private int GetCurrentDay()
        {
            if (DailyGameManager.Instance != null && DailyGameManager.Instance.CurrentState != null)
                return DailyGameManager.Instance.CurrentState.currentDay;

            return 1;
        }

        private void SubscribeToGameManager()
        {
            if (subscribedToGameManager || !listenToGameManager || DailyGameManager.Instance == null)
                return;

            DailyGameManager.Instance.StateChanged += RefreshForCurrentDay;

            if (DailyGameManager.Instance.TaskManager != null)
                DailyGameManager.Instance.TaskManager.TasksChanged += RefreshForCurrentDay;

            subscribedToGameManager = true;
        }

        private void UnsubscribeFromGameManager()
        {
            if (!subscribedToGameManager || DailyGameManager.Instance == null)
                return;

            DailyGameManager.Instance.StateChanged -= RefreshForCurrentDay;

            if (DailyGameManager.Instance.TaskManager != null)
                DailyGameManager.Instance.TaskManager.TasksChanged -= RefreshForCurrentDay;

            subscribedToGameManager = false;
        }
    }

    [Serializable]
    public class BossPhoneCallCollection
    {
        public BossPhoneCallData[] calls;
    }

    [Serializable]
    public class BossPhoneCallData
    {
        public int day;
        public string callId;
        public float ringDelay;
        public string firstNodeId;
        public BossPhoneNodeData[] nodes;
    }

    [Serializable]
    public class BossPhoneNodeData
    {
        public string id;
        [TextArea(2, 4)] public string text;
        public string next;
        public int unlockClueId;
        public BossPhoneChoiceData[] choices;
    }

    [Serializable]
    public class BossPhoneChoiceData
    {
        public string text;
        public string next;
        public int unlockClueId;
    }
}
