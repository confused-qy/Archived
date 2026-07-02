using UnityEngine;

namespace EmployeeHandbook.Feishu
{
    /// <summary>
    /// Controls which page is visible inside the Feishu window.
    /// Put all switchable pages under Pages Root, then call ShowPage from Feishu buttons.
    /// </summary>
    public class FeishuPageManager : MonoBehaviour
    {
        private const string DefaultPagesRootName = "Pages";

        [SerializeField] private Transform pagesRoot;
        [SerializeField] private bool showFirstPageOnStart = true;
        [SerializeField] private bool warnWhenPageMissing = true;

        private GameObject currentPage;

        private void Start()
        {
            EnsurePagesRoot();

            if (!showFirstPageOnStart)
                return;

            GameObject firstPage = GetFirstPage();
            if (firstPage != null)
                ShowPage(firstPage);
        }

        public void ShowPage(GameObject page)
        {
            if (page == null)
            {
                if (warnWhenPageMissing)
                    Debug.LogWarning("FeishuPageManager 收到空 Page，无法切换。", this);
                return;
            }

            if (!EnsurePagesRoot())
                return;

            if (!page.transform.IsChildOf(pagesRoot))
            {
                if (warnWhenPageMissing)
                    Debug.LogWarning("FeishuPageManager: " + page.name + " 不在 Pages Root 下面。", this);
                return;
            }

            for (int i = 0; i < pagesRoot.childCount; i++)
            {
                GameObject childPage = pagesRoot.GetChild(i).gameObject;
                bool shouldShow = childPage == page;
                childPage.SetActive(shouldShow);

                if (shouldShow)
                    currentPage = childPage;
            }
        }

        public void ShowPageByName(string pageName)
        {
            if (string.IsNullOrWhiteSpace(pageName))
            {
                if (warnWhenPageMissing)
                    Debug.LogWarning("FeishuPageManager 收到空 Page Name，无法切换。", this);
                return;
            }

            if (!EnsurePagesRoot())
                return;

            Transform pageTransform = pagesRoot.Find(pageName);
            if (pageTransform == null)
            {
                if (warnWhenPageMissing)
                    Debug.LogWarning("FeishuPageManager 找不到 Page: " + pageName, this);
                return;
            }

            ShowPage(pageTransform.gameObject);
        }

        public void HideAllPages()
        {
            if (!EnsurePagesRoot())
                return;

            for (int i = 0; i < pagesRoot.childCount; i++)
                pagesRoot.GetChild(i).gameObject.SetActive(false);

            currentPage = null;
        }

        private bool EnsurePagesRoot()
        {
            if (pagesRoot != null)
                return true;

            Transform foundRoot = FindChildByName(transform, DefaultPagesRootName);
            if (foundRoot != null)
            {
                pagesRoot = foundRoot;
                return true;
            }

            if (warnWhenPageMissing)
                Debug.LogWarning("FeishuPageManager 找不到 Pages Root，请在 Inspector 里拖入 Pages。", this);

            return false;
        }

        private GameObject GetFirstPage()
        {
            if (pagesRoot == null || pagesRoot.childCount == 0)
                return null;

            return pagesRoot.GetChild(0).gameObject;
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
