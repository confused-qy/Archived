using UnityEngine;

namespace EmployeeHandbook.Feishu
{
    [RequireComponent(typeof(AudioSource))]
    public class FeishuSfxPlayer : MonoBehaviour
    {
        public static FeishuSfxPlayer Instance { get; private set; }

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;

        [Header("Clips")]
        [SerializeField] private AudioClip tabClickClip;
        [SerializeField] private AudioClip closeClickClip;
        [SerializeField] private AudioClip openChatClip;
        [SerializeField] private AudioClip sendMessageClip;
        [SerializeField] private AudioClip openFriendProfileClip;
        [SerializeField] private AudioClip deleteClickClip;
        [SerializeField] private AudioClip deleteSuccessClip;
        [SerializeField] private AudioClip deleteFailureClip;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("场景中存在多个 FeishuSfxPlayer，将使用最新启用的这个。", this);
            }

            Instance = this;

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        public void PlayTabClick()
        {
            Play(tabClickClip);
        }

        public void PlayCloseClick()
        {
            Play(closeClickClip);
        }

        public void PlayOpenChat()
        {
            Play(openChatClip);
        }

        public void PlaySendMessage()
        {
            Play(sendMessageClip);
        }

        public void PlayOpenFriendProfile()
        {
            Play(openFriendProfileClip);
        }

        public void PlayDeleteClick()
        {
            Play(deleteClickClip);
        }

        public void PlayDeleteSuccess()
        {
            Play(deleteSuccessClip);
        }

        public void PlayDeleteFailure()
        {
            Play(deleteFailureClip);
        }

        public static void PlayTabClickSfx()
        {
            if (Instance != null)
                Instance.PlayTabClick();
        }

        public static void PlayCloseClickSfx()
        {
            if (Instance != null)
                Instance.PlayCloseClick();
        }

        public static void PlayOpenChatSfx()
        {
            if (Instance != null)
                Instance.PlayOpenChat();
        }

        public static void PlaySendMessageSfx()
        {
            if (Instance != null)
                Instance.PlaySendMessage();
        }

        public static void PlayOpenFriendProfileSfx()
        {
            if (Instance != null)
                Instance.PlayOpenFriendProfile();
        }

        public static void PlayDeleteClickSfx()
        {
            if (Instance != null)
                Instance.PlayDeleteClick();
        }

        public static void PlayDeleteSuccessSfx()
        {
            if (Instance != null)
                Instance.PlayDeleteSuccess();
        }

        public static void PlayDeleteFailureSfx()
        {
            if (Instance != null)
                Instance.PlayDeleteFailure();
        }

        private void Play(AudioClip clip)
        {
            if (clip == null || audioSource == null)
                return;

            audioSource.PlayOneShot(clip);
        }
    }
}
