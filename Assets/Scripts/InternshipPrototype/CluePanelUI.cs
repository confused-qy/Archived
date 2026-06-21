using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace EmployeeHandbook
{
    /// <summary>Renders all unlocked clue strings in one simple text panel.</summary>
    public class CluePanelUI : MonoBehaviour
    {
        [SerializeField] private Text clueText;

        private void Start()
        {
            if (GameManager.Instance == null || clueText == null)
            {
                Debug.LogWarning(
                    "CluePanelUI is not configured, so it has been disabled.",
                    this);
                enabled = false;
                return;
            }

            GameManager.Instance.StateChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.StateChanged -= Refresh;
        }

        public void Refresh()
        {
            if (clueText == null || GameManager.Instance == null)
                return;

            if (GameManager.Instance.DiscoveredClues.Count == 0)
            {
                clueText.text = "No fragments recovered.";
                return;
            }

            StringBuilder builder = new StringBuilder();
            foreach (string clue in GameManager.Instance.DiscoveredClues)
                builder.Append("• ").AppendLine(clue).AppendLine();

            clueText.text = builder.ToString();
        }
    }
}
