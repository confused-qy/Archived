using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmployeeHandbook.OfficeGames
{
    public class ExcelAnomalyCellView : MonoBehaviour
    {
        [SerializeField] private string cellId;
        [SerializeField] private Button button;
        [SerializeField] private Text cellText;
        [SerializeField] private TMP_Text cellTmpText;
        [SerializeField] private GameObject selectedFrame;

        private Action<string> clicked;
        private bool selected;

        public string CellId
        {
            get { return cellId; }
        }

        private void Awake()
        {
            AutoFindReferences();
            BindButton();
            SetSelected(false);
        }

        public void Initialize(string id, Action<string> onClicked)
        {
            AutoFindReferences();

            cellId = string.IsNullOrWhiteSpace(id) ? gameObject.name : id;
            clicked = onClicked;
            BindButton();
            SetSelected(false);
        }

        public void SetText(string value)
        {
            if (cellText != null)
                cellText.text = value;

            if (cellTmpText != null)
                cellTmpText.text = value;
        }

        public void SetSelected(bool value)
        {
            selected = value;

            if (selectedFrame != null)
                selectedFrame.SetActive(selected);
        }

        public void SetInteractable(bool interactable)
        {
            if (button != null)
                button.interactable = interactable;
        }

        public void Clear()
        {
            SetText(string.Empty);
            SetSelected(false);
            SetInteractable(false);
        }

        private void HandleClicked()
        {
            clicked?.Invoke(cellId);
        }

        private void BindButton()
        {
            if (button == null)
                return;

            button.onClick.RemoveListener(HandleClicked);
            button.onClick.AddListener(HandleClicked);
        }

        private void AutoFindReferences()
        {
            if (string.IsNullOrWhiteSpace(cellId))
                cellId = gameObject.name;

            if (button == null)
                button = GetComponent<Button>();

            if (button == null)
                button = GetComponentInChildren<Button>(true);

            if (cellTmpText == null)
                cellTmpText = GetComponentInChildren<TMP_Text>(true);

            if (cellText == null)
                cellText = GetComponentInChildren<Text>(true);

            if (selectedFrame == null)
            {
                Transform frame = transform.Find("SelectedFrame");
                if (frame != null)
                    selectedFrame = frame.gameObject;
            }
        }
    }
}
