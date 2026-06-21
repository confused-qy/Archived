using System;
using UnityEngine;
using UnityEngine.UI;

namespace EmployeeHandbook
{
    /// <summary>Reusable confirmation popup for dangerous or abnormal actions.</summary>
    public class WarningDialog : MonoBehaviour
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text messageText;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button returnButton;

        private Action onContinue;

        private void Awake()
        {
            continueButton.onClick.AddListener(Continue);
            returnButton.onClick.AddListener(Return);
            gameObject.SetActive(false);
        }

        public void Show(string title, string message, Action continueAction)
        {
            titleText.text = title;
            messageText.text = message;
            onContinue = continueAction;
            returnButton.gameObject.SetActive(true);
            gameObject.SetActive(true);
        }

        public void ShowMessage(string title, string message, Action closeAction)
        {
            titleText.text = title;
            messageText.text = message;
            onContinue = closeAction;
            returnButton.gameObject.SetActive(false);
            gameObject.SetActive(true);
        }

        private void Continue()
        {
            Action callback = onContinue;
            Close();
            callback?.Invoke();
        }

        private void Return()
        {
            Close();
        }

        private void Close()
        {
            onContinue = null;
            gameObject.SetActive(false);
        }
    }
}
