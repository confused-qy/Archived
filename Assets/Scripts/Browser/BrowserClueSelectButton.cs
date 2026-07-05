using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using TMPro;

namespace EmployeeHandbook.Browser
{
    /// <summary>
    /// Attach this to a Button in the notebook. It sends its id and display text to the browser search controller.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class BrowserClueSelectButton : MonoBehaviour
    {
        [FormerlySerializedAs("entryId")]
        [SerializeField] private int clueId;
        [SerializeField] private string queryText;
        [SerializeField] private BrowserClueSearchController browserSearchController;
        [SerializeField] private Button button;
        [SerializeField] private Text buttonText;
        [SerializeField] private TMP_Text buttonTmpText;
        [SerializeField] private bool autoBindButton = true;
        [SerializeField] private bool useButtonTextWhenQueryEmpty = true;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();

            AutoBindText();

            if (autoBindButton && button != null)
            {
                button.onClick.RemoveListener(SelectQuery);
                button.onClick.AddListener(SelectQuery);
            }
        }

        public void SelectClue()
        {
            SelectQuery();
        }

        public void ConfigureClue(int configuredClueId, string configuredQueryText)
        {
            clueId = configuredClueId;

            if (!string.IsNullOrWhiteSpace(configuredQueryText))
                queryText = configuredQueryText;
        }

        public void SelectQuery()
        {
            if (browserSearchController == null)
            {
                Debug.LogWarning("BrowserClueSelectButton 缺少 Browser Search Controller。", this);
                return;
            }

            browserSearchController.SelectClue(GetClueId(), GetQueryText());
        }

        private string GetQueryText()
        {
            if (!string.IsNullOrWhiteSpace(queryText))
                return queryText;

            if (!useButtonTextWhenQueryEmpty)
                return string.Empty;

            if (buttonTmpText != null)
                return buttonTmpText.text;

            if (buttonText != null)
                return buttonText.text;

            return string.Empty;
        }

        private int GetClueId()
        {
            return clueId;
        }

        private void AutoBindText()
        {
            if (buttonText == null)
                buttonText = GetComponentInChildren<Text>(true);

            if (buttonTmpText == null)
                buttonTmpText = GetComponentInChildren<TMP_Text>(true);
        }
    }
}
