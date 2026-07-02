using TMPro;
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
        [SerializeField] private ClueEntry[] clues;
        [SerializeField] private int[] initiallyUnlockedClueIds;

        private void Awake()
        {
            CacheOriginalNames();
            LockAllClues();
            UnlockInitialClues();
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
            if (clues == null)
                return;

            for (int i = 0; i < clues.Length; i++)
            {
                clues[i].AssignDefaultId(i + 1);
                clues[i].SetUnlocked(false);
            }
        }

        public bool IsClueUnlocked(int clueId)
        {
            ClueEntry clue = FindClue(clueId);
            return clue != null && clue.Unlocked;
        }

        public string GetClueName(int clueId)
        {
            ClueEntry clue = FindClue(clueId);
            return clue != null ? clue.OriginalName : string.Empty;
        }

        public void SetClueUnlocked(int clueId, bool unlocked)
        {
            ClueEntry clue = FindClue(clueId);
            if (clue == null)
            {
                Debug.LogWarning("ClueNotebookClueList 找不到线索编号：" + clueId, this);
                return;
            }

            clue.SetUnlocked(unlocked);
        }

        private void CacheOriginalNames()
        {
            if (clues == null)
                return;

            for (int i = 0; i < clues.Length; i++)
            {
                clues[i].AssignDefaultId(i + 1);
                clues[i].CacheOriginalName();
            }
        }

        private void UnlockInitialClues()
        {
            if (initiallyUnlockedClueIds == null)
                return;

            for (int i = 0; i < initiallyUnlockedClueIds.Length; i++)
                UnlockClue(initiallyUnlockedClueIds[i]);
        }

        private ClueEntry FindClue(int clueId)
        {
            if (clues == null)
                return null;

            for (int i = 0; i < clues.Length; i++)
            {
                clues[i].AssignDefaultId(i + 1);
                if (clues[i].ClueId == clueId)
                    return clues[i];
            }

            return null;
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

            public void SetUnlocked(bool value)
            {
                unlocked = value;

                GameObject targetObject = GetTargetObject();
                if (targetObject != null)
                    targetObject.SetActive(unlocked);
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
