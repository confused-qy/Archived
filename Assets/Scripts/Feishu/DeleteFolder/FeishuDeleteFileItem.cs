using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmployeeHandbook.Feishu.DeleteFolder
{
    public class FeishuDeleteFileItem : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Text nameText;
        [SerializeField] private TMP_Text nameTmpText;
        [SerializeField] private GameObject selectedFrame;
        [SerializeField] private GameObject deletedOverlay;

        private string fileId;
        private bool shouldDelete;
        private bool selected;
        private bool deleted;
        private bool canSelect;
        private Action<FeishuDeleteFileItem> clicked;

        public string FileId
        {
            get { return fileId; }
        }

        public bool ShouldDelete
        {
            get { return shouldDelete; }
        }

        public bool Selected
        {
            get { return selected; }
        }

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (button != null)
                button.onClick.AddListener(HandleClick);
        }

        public void Setup(string id, string displayName, bool targetShouldDelete, Sprite sprite, bool allowSelection, bool isDeleted, Action<FeishuDeleteFileItem> onClicked)
        {
            fileId = id;
            shouldDelete = targetShouldDelete;
            canSelect = allowSelection;
            deleted = isDeleted;
            selected = false;
            clicked = onClicked;

            SetName(TrimName(displayName));

            if (button != null)
            {
                button.interactable = true;
                if (sprite != null && button.image != null)
                    button.image.sprite = sprite;
            }

            RefreshVisual();
        }

        public void SetSelected(bool value)
        {
            if (deleted || !canSelect)
                value = false;

            selected = value;
            RefreshVisual();
        }

        public void SetDeleted(bool value)
        {
            deleted = value;
            selected = false;

            if (button != null)
                button.interactable = true;

            RefreshVisual();
        }

        private void HandleClick()
        {
            if (!canSelect || deleted)
                return;

            clicked?.Invoke(this);
        }

        private void SetName(string value)
        {
            if (nameTmpText != null)
                nameTmpText.text = value;

            if (nameText != null)
                nameText.text = value;
        }

        private void RefreshVisual()
        {
            if (selectedFrame != null)
                selectedFrame.SetActive(selected && !deleted);

            if (deletedOverlay != null)
                deletedOverlay.SetActive(deleted);
        }

        private string TrimName(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value.Length <= 16 ? value : value.Substring(0, 16);
        }
    }
}
