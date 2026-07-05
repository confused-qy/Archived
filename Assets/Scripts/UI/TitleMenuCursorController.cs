using UnityEngine;
using UnityEngine.EventSystems;

namespace EmployeeHandbook.DailyTasks
{
    /// <summary>
    /// 标题菜单光标控制：选中哪个菜单项，就只显示对应光标。
    /// </summary>
    public class TitleMenuCursorController : MonoBehaviour
    {
        [SerializeField] private GameObject defaultCursor;
        [SerializeField] private CursorItem[] cursorItems;
        [SerializeField] private bool hideAllOnStart = true;

        private GameObject activeCursor;

        private void Awake()
        {
            BindItems();

            if (hideAllOnStart)
                HideAllCursors();

            if (defaultCursor != null)
                ShowCursor(defaultCursor);
        }

        public void ShowCursor(GameObject cursor)
        {
            HideAllCursors();

            if (cursor == null)
                return;

            cursor.SetActive(true);
            activeCursor = cursor;
        }

        public void HideAllCursors()
        {
            if (defaultCursor != null)
                defaultCursor.SetActive(false);

            if (cursorItems != null)
            {
                for (int i = 0; i < cursorItems.Length; i++)
                {
                    if (cursorItems[i] != null && cursorItems[i].cursor != null)
                        cursorItems[i].cursor.SetActive(false);
                }
            }

            activeCursor = null;
        }

        private void BindItems()
        {
            if (cursorItems == null)
                return;

            for (int i = 0; i < cursorItems.Length; i++)
            {
                CursorItem item = cursorItems[i];
                if (item == null || item.target == null || item.cursor == null)
                    continue;

                TitleMenuCursorTarget target = item.target.GetComponent<TitleMenuCursorTarget>();
                if (target == null)
                    target = item.target.AddComponent<TitleMenuCursorTarget>();

                target.Setup(this, item.cursor);
            }
        }

        [System.Serializable]
        public class CursorItem
        {
            public GameObject target;
            public GameObject cursor;
        }
    }

    public class TitleMenuCursorTarget : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, ISelectHandler
    {
        private TitleMenuCursorController controller;
        private GameObject cursor;

        public void Setup(TitleMenuCursorController controller, GameObject cursor)
        {
            this.controller = controller;
            this.cursor = cursor;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Show();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            Show();
        }

        public void OnSelect(BaseEventData eventData)
        {
            Show();
        }

        private void Show()
        {
            if (controller != null)
                controller.ShowCursor(cursor);
        }
    }
}
