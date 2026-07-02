using UnityEngine;
using DailyGameManager = EmployeeHandbook.DailyTasks.GameManager;

namespace EmployeeHandbook.Feishu
{
    public class FeishuContactListLayout : MonoBehaviour
    {
        [SerializeField] private ContactListItem[] listItems;
        [SerializeField] private int currentDay = 1;
        [SerializeField] private bool useGameManagerDay = true;
        [SerializeField] private float spacing = 0f;
        [SerializeField] private bool useFirstItemPositionAsTop = true;
        [SerializeField] private Vector2 topAnchoredPosition;

        private bool positionsCached;

        private void Awake()
        {
            CacheOriginalPositions();
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void Start()
        {
            Refresh();
        }

        public void LoadDay(int day)
        {
            currentDay = Mathf.Max(1, day);
            Rebuild();
        }

        public void Refresh()
        {
            RefreshCurrentDay();
            Rebuild();
        }

        public void Rebuild()
        {
            CacheOriginalPositions();
            DisableLayoutGroup();

            if (listItems == null)
                return;

            int visibleIndex = 0;
            for (int i = 0; i < listItems.Length; i++)
            {
                ContactListItem item = listItems[i];
                if (item == null || item.panel == null)
                    continue;

                bool unlocked = currentDay >= item.unlockDay;
                item.panel.SetActive(unlocked);
                if (unlocked)
                {
                    RectTransform itemRect = item.panel.transform as RectTransform;
                    if (itemRect != null)
                    {
                        float itemHeight = item.GetHeight();
                        itemRect.anchoredPosition = new Vector2(item.originalAnchoredPosition.x,
                            topAnchoredPosition.y - visibleIndex * (itemHeight + spacing));
                    }

                    visibleIndex++;
                }
            }
        }

        private void CacheOriginalPositions()
        {
            if (positionsCached || listItems == null)
                return;

            for (int i = 0; i < listItems.Length; i++)
            {
                if (listItems[i] != null)
                    listItems[i].CacheOriginalPosition();
            }

            if (useFirstItemPositionAsTop)
            {
                for (int i = 0; i < listItems.Length; i++)
                {
                    if (listItems[i] != null && listItems[i].panel != null)
                    {
                        topAnchoredPosition = listItems[i].originalAnchoredPosition;
                        break;
                    }
                }
            }

            positionsCached = true;
        }

        private void DisableLayoutGroup()
        {
            UnityEngine.UI.VerticalLayoutGroup verticalLayoutGroup = GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            if (verticalLayoutGroup != null)
                verticalLayoutGroup.enabled = false;
        }

        private void RefreshCurrentDay()
        {
            if (!useGameManagerDay || DailyGameManager.Instance == null || DailyGameManager.Instance.CurrentState == null)
                return;

            currentDay = Mathf.Max(1, DailyGameManager.Instance.CurrentState.currentDay);
        }

        [System.Serializable]
        private class ContactListItem
        {
            public GameObject panel;
            public int unlockDay = 1;

            [HideInInspector] public Vector2 originalAnchoredPosition;
            [HideInInspector] public Vector2 originalSize;

            public void CacheOriginalPosition()
            {
                RectTransform rectTransform = panel != null ? panel.transform as RectTransform : null;
                if (rectTransform == null)
                    return;

                originalAnchoredPosition = rectTransform.anchoredPosition;
                originalSize = rectTransform.rect.size;
                if (originalSize.y <= 0f)
                    originalSize = rectTransform.sizeDelta;
            }

            public float GetHeight()
            {
                return Mathf.Max(0f, originalSize.y);
            }
        }
    }
}
