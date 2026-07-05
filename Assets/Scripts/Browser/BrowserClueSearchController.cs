using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using EmployeeHandbook.ClueSystem;

namespace EmployeeHandbook.Browser
{
    /// <summary>
    /// Browser-side search flow:
    /// open notebook for selection, put the clicked notebook button text into the browser field,
    /// then search to show its description.
    /// </summary>
    public class BrowserClueSearchController : MonoBehaviour
    {
        [SerializeField] private ClueNotebookController notebookController;
        [SerializeField] private ClueNotebookClueList clueList;
        [SerializeField] private string clueDatabaseResourceName = ClueDatabase.DefaultResourceName;
        [FormerlySerializedAs("clueSearchEntries")]
        [SerializeField] private SearchEntry[] searchEntries;
        [SerializeField] private InputField queryInputField;
        [SerializeField] private TMP_InputField queryTmpInputField;
        [SerializeField] private Text queryText;
        [SerializeField] private TMP_Text queryTmpText;
        [SerializeField] private Text resultText;
        [SerializeField] private TMP_Text resultTmpText;
        [SerializeField] private string emptyQueryText = "";
        [SerializeField] private string noQueryMessage = "请输入内容。";
        [SerializeField] private string missingDescriptionMessage = "没有找到相关搜索结果。";
        [SerializeField] private bool resetWhenBrowserOpens = true;
        [SerializeField] private bool closeNotebookWhenBrowserCloses = true;
        [HideInInspector] [SerializeField] private bool closeNotebookAfterSelect;

        private int selectedClueId = -1;
        private string selectedQuery = "";
        private bool notebookOpenedByThisBrowser;
        private ClueDatabaseData clueDatabase;

        private void Awake()
        {
            clueDatabase = ClueDatabase.Load(clueDatabaseResourceName);
        }

        private void OnEnable()
        {
            if (resetWhenBrowserOpens)
                ClearSelection();
        }

        private void Start()
        {
            SetQueryText(emptyQueryText);
            SetResultText(string.Empty);
        }

        private void OnDisable()
        {
            if (closeNotebookWhenBrowserCloses)
                CloseNotebookOpenedByBrowser();
        }

        public void OpenNotebookForInputSelection()
        {
            OpenNotebookForClueSelection();
        }

        public void OpenNotebookForClueSelection()
        {
            if (notebookController == null)
            {
                Debug.LogWarning("BrowserClueSearchController 缺少 Notebook Controller。", this);
                return;
            }

            notebookController.OpenNotebookFromBrowser();
            notebookOpenedByThisBrowser = true;
        }

        public void SelectClue(int clueId)
        {
            ClueDefinition definition = ClueDatabase.FindById(clueDatabase, clueId);
            SearchEntry entry = definition == null ? FindEntryByClueId(clueId) : null;
            string query = definition != null && !string.IsNullOrWhiteSpace(definition.name)
                ? definition.name
                : entry != null && !string.IsNullOrWhiteSpace(entry.QueryText)
                    ? entry.QueryText
                    : GetFallbackQueryText(clueId);

            SelectQuery(clueId, query);
        }

        public void SelectClue(int clueId, string query)
        {
            SelectQuery(clueId, query);
        }

        public void SelectQuery(int clueId, string query)
        {
            selectedClueId = clueId;
            selectedQuery = query;
            SetQueryText(query);
            SetResultText(string.Empty);
        }

        public void SearchSelectedClue()
        {
            SearchCurrentQuery();
        }

        public void SearchCurrentQuery()
        {
            string query = GetQueryText();
            if (string.IsNullOrWhiteSpace(query))
            {
                SetResultText(noQueryMessage);
                return;
            }

            ClueDefinition definition = selectedClueId > 0
                ? ClueDatabase.FindById(clueDatabase, selectedClueId)
                : ClueDatabase.FindByQuery(clueDatabase, query);
            if (definition != null && !definition.searchable)
            {
                SetResultText(missingDescriptionMessage);
                return;
            }

            SearchEntry entry = definition == null
                ? selectedClueId > 0
                    ? FindEntryByClueId(selectedClueId)
                    : FindEntryByQuery(query)
                : null;

            string description = definition != null ? definition.description : entry != null ? entry.Description : string.Empty;
            if (string.IsNullOrWhiteSpace(description))
            {
                SetResultText(missingDescriptionMessage);
                return;
            }

            SetResultText(description);
            UnlockClueFromSearch(definition);
        }

        public void ClearSelection()
        {
            selectedClueId = -1;
            selectedQuery = "";
            SetQueryText(emptyQueryText);
            SetResultText(string.Empty);
        }

        public void CloseNotebookOpenedByBrowser()
        {
            if (!notebookOpenedByThisBrowser || notebookController == null)
                return;

            notebookController.CloseNotebookFromBrowser();
            notebookOpenedByThisBrowser = false;
        }

        private SearchEntry FindEntryByClueId(int clueId)
        {
            SearchEntry[] entries = GetEntries();
            if (entries == null)
                return null;

            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i] != null && entries[i].ClueId == clueId)
                    return entries[i];
            }

            return null;
        }

        private SearchEntry FindEntryByQuery(string query)
        {
            SearchEntry[] entries = GetEntries();
            if (entries == null)
                return null;

            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i] != null && entries[i].MatchesQuery(query))
                    return entries[i];
            }

            return null;
        }

        private string GetFallbackQueryText(int clueId)
        {
            string clueName = clueList != null ? clueList.GetClueName(clueId) : string.Empty;
            return string.IsNullOrWhiteSpace(clueName) ? "线索 " + clueId : clueName;
        }

        private string GetQueryText()
        {
            if (queryTmpInputField != null)
                return queryTmpInputField.text;

            if (queryInputField != null)
                return queryInputField.text;

            if (queryTmpText != null)
                return queryTmpText.text;

            if (queryText != null)
                return queryText.text;

            return selectedQuery;
        }

        private SearchEntry[] GetEntries()
        {
            return searchEntries;
        }

        private void SetQueryText(string value)
        {
            if (queryInputField != null)
                queryInputField.text = value;

            if (queryTmpInputField != null)
                queryTmpInputField.text = value;

            if (queryText != null)
                queryText.text = value;

            if (queryTmpText != null)
                queryTmpText.text = value;
        }

        private void SetResultText(string value)
        {
            if (resultText != null)
                resultText.text = value;

            if (resultTmpText != null)
                resultTmpText.text = value;
        }

        private void UnlockClueFromSearch(ClueDefinition definition)
        {
            if (definition == null || definition.unlockClueIdOnSearch <= 0 || clueList == null)
                return;

            clueList.UnlockClue(definition.unlockClueIdOnSearch);
        }

        [System.Serializable]
        private class SearchEntry
        {
            [FormerlySerializedAs("entryId")]
            [SerializeField] private int clueId;
            [SerializeField] private string queryText;
            [TextArea(2, 6)] [SerializeField] private string description;

            public int ClueId
            {
                get { return clueId; }
            }

            public string QueryText
            {
                get { return queryText; }
            }

            public string Description
            {
                get { return description; }
            }

            public bool MatchesQuery(string query)
            {
                return string.Equals(queryText != null ? queryText.Trim() : string.Empty,
                    query != null ? query.Trim() : string.Empty,
                    System.StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
