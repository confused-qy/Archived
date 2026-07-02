using UnityEngine;
using UnityEngine.UI;

namespace EmployeeHandbook.Feishu
{
    /// <summary>
    /// Changes a Feishu tab button image between normal and selected states.
    /// Keep the Button RectTransform fixed; resize only the target Image if the two sprites differ in size.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class FeishuTabButtonVisual : MonoBehaviour
    {
        [Header("Two Image Objects")]
        [SerializeField] private GameObject normalImageObject;
        [SerializeField] private GameObject selectedImageObject;

        [Header("Click Binding")]
        [SerializeField] private Button button;
        [SerializeField] private bool autoBindButtonClick = true;

        [Header("Single Image Sprite Swap")]
        [SerializeField] private Image targetImage;
        [SerializeField] private Sprite normalSprite;
        [SerializeField] private Sprite selectedSprite;
        [SerializeField] private bool selectedOnStart;

        [Header("Optional Size Override")]
        [SerializeField] private bool overrideImageSize = true;
        [SerializeField] private Vector2 normalSize = new Vector2(80f, 32f);
        [SerializeField] private Vector2 selectedSize = new Vector2(92f, 36f);

        [Header("Optional Group")]
        [SerializeField] private bool autoCollectGroupFromParent = true;
        [SerializeField] private Transform groupRoot;
        [SerializeField] private FeishuTabButtonVisual[] groupButtons;

        private RectTransform imageRectTransform;
        private bool selected;

        private void Awake()
        {
            CacheReferences();
            CacheGroupButtons();
            BindButtonClick();
        }

        private void Start()
        {
            if (selectedOnStart)
                SelectThis();
            else
                SetSelected(false);
        }

        public void SelectThis()
        {
            CacheGroupButtons();

            if (groupButtons != null && groupButtons.Length > 0)
            {
                for (int i = 0; i < groupButtons.Length; i++)
                {
                    if (groupButtons[i] != null)
                        groupButtons[i].SetSelected(groupButtons[i] == this);
                }

                return;
            }

            SetSelected(true);
        }

        public void SetSelected(bool value)
        {
            selected = value;
            RefreshVisual();
        }

        public void SetNormal()
        {
            SetSelected(false);
        }

        public void SetSelectedState()
        {
            SelectThis();
        }

        private void RefreshVisual()
        {
            CacheReferences();

            bool usesTwoObjects = normalImageObject != null || selectedImageObject != null;

            if (normalImageObject != null)
                normalImageObject.SetActive(!selected);

            if (selectedImageObject != null)
                selectedImageObject.SetActive(selected);

            if (!usesTwoObjects && targetImage != null)
                targetImage.sprite = selected ? selectedSprite : normalSprite;

            if (!usesTwoObjects && overrideImageSize && imageRectTransform != null)
                imageRectTransform.sizeDelta = selected ? selectedSize : normalSize;
        }

        private void CacheReferences()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (targetImage == null)
                targetImage = GetComponent<Image>();

            if (targetImage == null)
                targetImage = GetComponentInChildren<Image>(true);

            if (targetImage != null && imageRectTransform == null)
                imageRectTransform = targetImage.rectTransform;
        }

        private void CacheGroupButtons()
        {
            if (groupButtons != null && groupButtons.Length > 0)
                return;

            if (!autoCollectGroupFromParent)
                return;

            Transform root = groupRoot != null ? groupRoot : transform.parent;
            if (root == null)
                return;

            groupButtons = root.GetComponentsInChildren<FeishuTabButtonVisual>(true);
        }

        private void BindButtonClick()
        {
            if (!autoBindButtonClick || button == null)
                return;

            button.onClick.RemoveListener(SelectThis);
            button.onClick.AddListener(SelectThis);
        }
    }
}
