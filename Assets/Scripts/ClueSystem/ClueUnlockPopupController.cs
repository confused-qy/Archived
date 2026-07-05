using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmployeeHandbook.ClueSystem
{
    public class ClueUnlockPopupController : MonoBehaviour
    {
        [SerializeField] private GameObject popupPanel;
        [SerializeField] private Text messageText;
        [SerializeField] private TMP_Text messageTmpText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private string messageFormat = "解锁了线索：{0}！";
        [SerializeField] private bool hideOnStart = true;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip unlockSound;

        private void Awake()
        {
            EnsureReferences();

            if (confirmButton != null)
                confirmButton.onClick.AddListener(Hide);

            if (hideOnStart)
                Hide();
        }

        public void Show(string clueName)
        {
            EnsureReferences();

            if (string.IsNullOrWhiteSpace(clueName))
                clueName = "未知线索";

            SetMessage(string.Format(messageFormat, clueName));

            if (popupPanel != null)
            {
                EnsureParentsActive(popupPanel.transform);
                popupPanel.SetActive(true);
                BringToFront(popupPanel.transform);
            }

            PlayUnlockSound();
        }

        public void Hide()
        {
            if (popupPanel != null)
                popupPanel.SetActive(false);
        }

        private void SetMessage(string value)
        {
            if (messageText != null)
                messageText.text = value;

            if (messageTmpText != null)
                messageTmpText.text = value;
        }

        private void EnsureReferences()
        {
            if (popupPanel == null)
                popupPanel = gameObject;

            if (messageTmpText == null && popupPanel != null)
                messageTmpText = popupPanel.GetComponentInChildren<TMP_Text>(true);

            if (messageText == null && popupPanel != null)
                messageText = popupPanel.GetComponentInChildren<Text>(true);

            if (confirmButton == null && popupPanel != null)
                confirmButton = popupPanel.GetComponentInChildren<Button>(true);

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        private void PlayUnlockSound()
        {
            if (unlockSound == null)
                return;

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }

            if (!audioSource.enabled)
                audioSource.enabled = true;

            audioSource.PlayOneShot(unlockSound);
        }

        private static void EnsureParentsActive(Transform target)
        {
            if (target == null)
                return;

            Transform parent = target.parent;
            while (parent != null)
            {
                if (!parent.gameObject.activeSelf)
                    parent.gameObject.SetActive(true);

                parent = parent.parent;
            }
        }

        private static void BringToFront(Transform target)
        {
            Transform current = target;
            while (current != null)
            {
                current.SetAsLastSibling();
                current = current.parent;
            }
        }
    }
}
