using UnityEngine;
using UnityEngine.UI;

namespace EmployeeHandbook.ClueSystem
{
    /// <summary>
    /// Simple notebook page switcher. Only one page is visible at a time.
    /// Put page objects under Pages Root, or assign them manually in Pages.
    /// </summary>
    public class ClueNotebookPageController : MonoBehaviour
    {
        private const string DefaultPagesRootName = "Pages";

        [SerializeField] private Transform pagesRoot;
        [SerializeField] private GameObject[] pages;
        [SerializeField] private bool autoCollectPagesFromRoot = true;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private bool autoBindButtons = true;

        private int currentPageIndex;

        private void Awake()
        {
            EnsurePages();
            BindButtons();
        }

        private void Start()
        {
            ResetToFirstPage();
        }

        public void ResetToFirstPage()
        {
            ShowPageAtIndex(0);
        }

        public void ResetToFirstSpread()
        {
            ResetToFirstPage();
        }

        public void NextPage()
        {
            ShowPageAtIndex(currentPageIndex + 1);
        }

        public void PreviousPage()
        {
            ShowPageAtIndex(currentPageIndex - 1);
        }

        public void NextSpread()
        {
            NextPage();
        }

        public void PreviousSpread()
        {
            PreviousPage();
        }

        public void ShowPageAtIndex(int pageIndex)
        {
            if (!EnsurePages())
                return;

            currentPageIndex = Mathf.Clamp(pageIndex, 0, pages.Length - 1);

            for (int i = 0; i < pages.Length; i++)
            {
                if (pages[i] != null)
                    pages[i].SetActive(i == currentPageIndex);
            }

            RefreshButtons();
        }

        public void ShowPage(GameObject page)
        {
            if (page == null || !EnsurePages())
                return;

            for (int i = 0; i < pages.Length; i++)
            {
                if (pages[i] == page)
                {
                    ShowPageAtIndex(i);
                    return;
                }
            }

            Debug.LogWarning("ClueNotebookPageController: " + page.name + " 不在 Pages 列表里。", this);
        }

        public void ShowPageByName(string pageName)
        {
            if (string.IsNullOrWhiteSpace(pageName) || !EnsurePages())
                return;

            for (int i = 0; i < pages.Length; i++)
            {
                if (pages[i] != null && pages[i].name == pageName)
                {
                    ShowPageAtIndex(i);
                    return;
                }
            }

            Debug.LogWarning("ClueNotebookPageController 找不到页面：" + pageName, this);
        }

        private bool EnsurePages()
        {
            if (pages != null && pages.Length > 0)
                return true;

            if (!autoCollectPagesFromRoot)
                return false;

            if (pagesRoot == null)
                pagesRoot = FindChildByName(transform, DefaultPagesRootName);

            if (pagesRoot == null)
            {
                Debug.LogWarning("ClueNotebookPageController 找不到 Pages Root，请拖入页面父物体。", this);
                return false;
            }

            pages = new GameObject[pagesRoot.childCount];
            for (int i = 0; i < pagesRoot.childCount; i++)
                pages[i] = pagesRoot.GetChild(i).gameObject;

            return pages.Length > 0;
        }

        private void RefreshButtons()
        {
            if (previousButton != null)
                previousButton.interactable = currentPageIndex > 0;

            if (nextButton != null)
                nextButton.interactable = pages != null && currentPageIndex < pages.Length - 1;
        }

        private void BindButtons()
        {
            if (!autoBindButtons)
                return;

            if (previousButton != null)
            {
                previousButton.onClick.RemoveListener(PreviousPage);
                previousButton.onClick.AddListener(PreviousPage);
            }

            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(NextPage);
                nextButton.onClick.AddListener(NextPage);
            }
        }

        private static Transform FindChildByName(Transform root, string childName)
        {
            if (root == null)
                return null;

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != root && children[i].name == childName)
                    return children[i];
            }

            return null;
        }
    }
}
