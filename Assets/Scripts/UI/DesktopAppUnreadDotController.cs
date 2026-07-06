using EmployeeHandbook.Email;
using EmployeeHandbook.Feishu;
using UnityEngine;
using DailyGameManager = EmployeeHandbook.DailyTasks.GameManager;

namespace EmployeeHandbook.UI
{
    public class DesktopAppUnreadDotController : MonoBehaviour
    {
        [Header("Dots")]
        [SerializeField] private GameObject feishuDotObject;
        [SerializeField] private GameObject emailDotObject;

        [Header("Sources")]
        [SerializeField] private FeishuConversationManager feishuConversationManager;
        [SerializeField] private EmailController emailController;
        [SerializeField] private bool autoFindSources = true;
        [SerializeField] private bool refreshEveryFrame = true;

        private bool subscribedToGameManager;

        private void Awake()
        {
            FindSourcesIfNeeded();
            Refresh();
        }

        private void OnEnable()
        {
            SubscribeToGameManager();
            FindSourcesIfNeeded();
            Refresh();
        }

        private void Update()
        {
            if (refreshEveryFrame)
                Refresh();
        }

        private void OnDisable()
        {
            UnsubscribeFromGameManager();
        }

        public void Refresh()
        {
            FindSourcesIfNeeded();

            if (feishuDotObject != null)
                feishuDotObject.SetActive(feishuConversationManager != null && feishuConversationManager.HasAnyUnread());

            if (emailDotObject != null)
                emailDotObject.SetActive(emailController != null && emailController.HasUnreadUnlockedMail());
        }

        private void FindSourcesIfNeeded()
        {
            if (!autoFindSources)
                return;

            if (feishuConversationManager == null)
                feishuConversationManager = FindSceneObject<FeishuConversationManager>();

            if (emailController == null)
                emailController = FindSceneObject<EmailController>();
        }

        private T FindSceneObject<T>() where T : Component
        {
            T[] objects = Resources.FindObjectsOfTypeAll<T>();
            for (int i = 0; i < objects.Length; i++)
            {
                T item = objects[i];
                if (item != null && item.gameObject.scene.IsValid())
                    return item;
            }

            return null;
        }

        private void SubscribeToGameManager()
        {
            if (subscribedToGameManager || DailyGameManager.Instance == null)
                return;

            DailyGameManager.Instance.StateChanged += Refresh;
            subscribedToGameManager = true;
        }

        private void UnsubscribeFromGameManager()
        {
            if (!subscribedToGameManager || DailyGameManager.Instance == null)
                return;

            DailyGameManager.Instance.StateChanged -= Refresh;
            subscribedToGameManager = false;
        }
    }
}
