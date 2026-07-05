using TMPro;
using EmployeeHandbook.Browser;
using UnityEngine;
using UnityEngine.UI;

namespace EmployeeHandbook.ClueSystem
{
    /// <summary>
    /// Controls which clue buttons are visible in the notebook.
    /// Locked clues are hidden. Unlocked clues are shown and can be searched in the browser.
    /// </summary>
    public class ClueNotebookClueList : MonoBehaviour
    {
        [SerializeField] private string clueDatabaseResourceName = ClueDatabase.DefaultResourceName;
        [SerializeField] private ClueEntry[] clues;
        [SerializeField] private CategoryTitle[] categoryTitles;
        [SerializeField] private int[] initiallyUnlockedClueIds;
        [SerializeField] private ClueUnlockPopupController unlockPopupController;
        [SerializeField] private string lockedCategoryTitle = "???";

        private ClueDatabaseData clueDatabase;
        private bool suppressUnlockPopup;
        private bool initialized;

        private void Awake()
        {
            EnsureInitialized();
        }

        public void UnlockClue(int clueId)
        {
            SetClueUnlocked(clueId, true);
        }

        public void LockClue(int clueId)
        {
            SetClueUnlocked(clueId, false);
        }

        public void LockAllClues()
        {
            EnsureClueDatabase();

            if (clues == null)
                return;

            int defaultId = 1;
            for (int i = 0; i < clues.Length; i++)
            {
                if (!clues[i].IsConfigured)
                    continue;

                clues[i].AssignDefaultId(defaultId);
                defaultId++;
                clues[i].SetUnlocked(false);
            }
        }

        public bool IsClueUnlocked(int clueId)
        {
            EnsureInitialized();

            ClueEntry clue = FindClue(clueId);
            return clue != null && clue.Unlocked;
        }

        public bool WillUnlockClue(int clueId)
        {
            EnsureInitialized();

            ClueEntry clue = FindClue(clueId);
            return clue != null && !clue.Unlocked;
        }

        public bool WillUnlockAnyClue(int[] clueIds)
        {
            EnsureInitialized();

            if (clueIds == null)
                return false;

            for (int i = 0; i < clueIds.Length; i++)
            {
                int clueId = clueIds[i];
                if (clueId > 0 && WillUnlockClue(clueId))
                    return true;
            }

            return false;
        }

        public string GetClueName(int clueId)
        {
            EnsureInitialized();

            ClueDefinition definition = ClueDatabase.FindById(clueDatabase, clueId);
            if (definition != null && !string.IsNullOrWhiteSpace(definition.name))
                return definition.name;

            ClueEntry clue = FindClue(clueId);
            if (clue != null && !string.IsNullOrWhiteSpace(clue.OriginalName))
                return clue.OriginalName;

            return "线索 " + clueId;
        }

        public void SetClueUnlocked(int clueId, bool unlocked)
        {
            EnsureInitialized();

            ClueEntry clue = FindClue(clueId);
            if (clue == null)
            {
                Debug.LogWarning("ClueNotebookClueList 找不到线索编号：" + clueId, this);
                return;
            }

            bool wasUnlocked = clue.Unlocked;
            clue.SetUnlocked(unlocked);
            RefreshCategoryTitles();

            if (unlocked && !wasUnlocked && !suppressUnlockPopup)
                ShowUnlockPopup(clueId);
        }

        private void CacheOriginalNames()
        {
            if (clues == null)
                return;

            int defaultId = 1;
            for (int i = 0; i < clues.Length; i++)
            {
                if (!clues[i].IsConfigured)
                    continue;

                clues[i].AssignDefaultId(defaultId);
                defaultId++;
                clues[i].CacheOriginalName();
                clues[i].ApplyDefinition(ClueDatabase.FindById(clueDatabase, clues[i].ClueId));
            }

            CacheCategoryTitleOriginalNames();
        }

        private void UnlockInitialClues()
        {
            if (initiallyUnlockedClueIds == null)
                return;

            suppressUnlockPopup = true;
            for (int i = 0; i < initiallyUnlockedClueIds.Length; i++)
                UnlockClue(initiallyUnlockedClueIds[i]);
            suppressUnlockPopup = false;
        }

        private void ShowUnlockPopup(int clueId)
        {
            if (unlockPopupController == null)
                return;

            unlockPopupController.Show(GetClueName(clueId));
        }

        private void EnsureInitialized()
        {
            if (initialized)
                return;

            initialized = true;
            EnsureClueDatabase();
            CacheOriginalNames();
            LockAllClues();
            UnlockInitialClues();
            RefreshCategoryTitles();
        }

        private void EnsureClueDatabase()
        {
            if (clueDatabase != null && clueDatabase.clues != null && clueDatabase.clues.Length > 0)
                return;

            clueDatabase = ClueDatabase.Load(clueDatabaseResourceName);
        }

        private ClueEntry FindClue(int clueId)
        {
            if (clues == null)
                return null;

            int defaultId = 1;
            for (int i = 0; i < clues.Length; i++)
            {
                if (!clues[i].IsConfigured)
                    continue;

                clues[i].AssignDefaultId(defaultId);
                defaultId++;
                if (clues[i].ClueId == clueId)
                    return clues[i];
            }

            return null;
        }

        private void CacheCategoryTitleOriginalNames()
        {
            if (categoryTitles == null)
                return;

            for (int i = 0; i < categoryTitles.Length; i++)
            {
                if (categoryTitles[i] != null)
                    categoryTitles[i].CacheOriginalTitle();
            }
        }

        private void RefreshCategoryTitles()
        {
            if (categoryTitles == null)
                return;

            for (int i = 0; i < categoryTitles.Length; i++)
            {
                if (categoryTitles[i] != null)
                    categoryTitles[i].Refresh(this, lockedCategoryTitle);
            }
        }

        [System.Serializable]
        private class CategoryTitle
        {
            [SerializeField] private Text titleText;
            [SerializeField] private TMP_Text titleTmpText;
            [SerializeField] private int[] clueIds;

            private string originalTitle;

            public void CacheOriginalTitle()
            {
                if (!string.IsNullOrWhiteSpace(originalTitle))
                    return;

                if (titleTmpText != null)
                    originalTitle = titleTmpText.text;
                else if (titleText != null)
                    originalTitle = titleText.text;
            }

            public void Refresh(ClueNotebookClueList clueList, string lockedTitle)
            {
                CacheOriginalTitle();

                bool anyUnlocked = false;
                if (clueList != null && clueIds != null)
                {
                    for (int i = 0; i < clueIds.Length; i++)
                    {
                        int clueId = clueIds[i];
                        if (clueId > 0 && clueList.IsClueUnlocked(clueId))
                        {
                            anyUnlocked = true;
                            break;
                        }
                    }
                }

                string value = anyUnlocked ? originalTitle : lockedTitle;
                if (titleText != null)
                    titleText.text = value;

                if (titleTmpText != null)
                    titleTmpText.text = value;
            }
        }

        [System.Serializable]
        private class ClueEntry
        {
            [SerializeField] private int clueId;
            [SerializeField] private GameObject clueButtonObject;
            [SerializeField] private Button clueButton;
            [SerializeField] private Text nameText;
            [SerializeField] private TMP_Text nameTmpText;

            private string originalName;
            private bool unlocked;

            public int ClueId
            {
                get { return clueId; }
            }

            public bool Unlocked
            {
                get { return unlocked; }
            }

            public string OriginalName
            {
                get { return originalName; }
            }

            public bool IsConfigured
            {
                get
                {
                    return clueButtonObject != null ||
                           clueButton != null ||
                           nameText != null ||
                           nameTmpText != null;
                }
            }

            public void AssignDefaultId(int defaultId)
            {
                if (clueId <= 0)
                    clueId = defaultId;
            }

            public void CacheOriginalName()
            {
                if (nameTmpText != null)
                    originalName = nameTmpText.text;
                else if (nameText != null)
                    originalName = nameText.text;
                else if (clueButton != null)
                    originalName = clueButton.name;
                else if (clueButtonObject != null)
                    originalName = clueButtonObject.name;
            }

            public void ApplyDefinition(ClueDefinition definition)
            {
                if (definition == null)
                {
                    ApplyBrowserSelectButton(originalName);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(definition.name))
                {
                    originalName = definition.name;
                    SetDisplayName(definition.name);
                }

                ApplyBrowserSelectButton(originalName);
            }

            public void SetUnlocked(bool value)
            {
                unlocked = value;

                GameObject targetObject = GetTargetObject();
                if (targetObject != null)
                    targetObject.SetActive(unlocked);
            }

            private void SetDisplayName(string value)
            {
                if (nameText != null)
                    nameText.text = value;

                if (nameTmpText != null)
                    nameTmpText.text = value;
            }

            private void ApplyBrowserSelectButton(string queryText)
            {
                GameObject targetObject = GetTargetObject();
                if (targetObject == null)
                    return;

                BrowserClueSelectButton selectButton = targetObject.GetComponent<BrowserClueSelectButton>();
                if (selectButton == null)
                    selectButton = targetObject.GetComponentInChildren<BrowserClueSelectButton>(true);

                if (selectButton == null)
                    return;

                selectButton.ConfigureClue(clueId, queryText);
            }

            private GameObject GetTargetObject()
            {
                if (clueButtonObject != null)
                    return clueButtonObject;

                if (clueButton != null)
                    return clueButton.gameObject;

                if (nameTmpText != null)
                {
                    Button parentButton = nameTmpText.GetComponentInParent<Button>(true);
                    if (parentButton != null)
                        return parentButton.gameObject;
                }

                if (nameText != null)
                {
                    Button parentButton = nameText.GetComponentInParent<Button>(true);
                    if (parentButton != null)
                        return parentButton.gameObject;
                }

                if (nameTmpText != null)
                    return nameTmpText.gameObject;

                if (nameText != null)
                    return nameText.gameObject;

                return null;
            }
        }
    }
}
