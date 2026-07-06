using UnityEngine;

namespace EmployeeHandbook.UI
{
    /// <summary>
    /// Keeps this RectTransform centered inside its parent at a fixed aspect ratio.
    /// Put all gameplay UI under this object. Anything outside it becomes letterbox/pillarbox area.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class FixedAspectSafeArea : MonoBehaviour
    {
        [SerializeField] private float targetWidth = 16f;
        [SerializeField] private float targetHeight = 9f;
        [SerializeField] private bool updateEveryFrame = true;

        private RectTransform rectTransform;
        private Vector2 lastParentSize;
        private float lastTargetWidth;
        private float lastTargetHeight;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            Apply();
        }

        private void OnEnable()
        {
            rectTransform = GetComponent<RectTransform>();
            Apply();
        }

        private void Update()
        {
            if (!updateEveryFrame && Application.isPlaying)
                return;

            ApplyIfNeeded();
        }

        private void OnRectTransformDimensionsChange()
        {
            rectTransform = GetComponent<RectTransform>();
            Apply();
        }

        private void OnValidate()
        {
            targetWidth = Mathf.Max(0.01f, targetWidth);
            targetHeight = Mathf.Max(0.01f, targetHeight);
            rectTransform = GetComponent<RectTransform>();
            Apply();
        }

        public void Apply()
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            Vector2 parentSize = GetParentSize();
            if (parentSize.x <= 0f || parentSize.y <= 0f)
                return;

            float targetAspect = targetWidth / targetHeight;
            float parentAspect = parentSize.x / parentSize.y;

            float width;
            float height;

            if (parentAspect > targetAspect)
            {
                height = parentSize.y;
                width = height * targetAspect;
            }
            else
            {
                width = parentSize.x;
                height = width / targetAspect;
            }

            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(width, height);

            lastParentSize = parentSize;
            lastTargetWidth = targetWidth;
            lastTargetHeight = targetHeight;
        }

        private void ApplyIfNeeded()
        {
            Vector2 parentSize = GetParentSize();
            if (parentSize != lastParentSize || !Mathf.Approximately(targetWidth, lastTargetWidth) || !Mathf.Approximately(targetHeight, lastTargetHeight))
                Apply();
        }

        private Vector2 GetParentSize()
        {
            RectTransform parentRect = rectTransform != null ? rectTransform.parent as RectTransform : null;
            if (parentRect != null)
                return parentRect.rect.size;

            return new Vector2(Screen.width, Screen.height);
        }
    }
}
