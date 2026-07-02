using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmployeeHandbook.Feishu
{
    public class FeishuDateDividerView : MonoBehaviour
    {
        [SerializeField] private Text dateText;
        [SerializeField] private TMP_Text dateTmpText;

        public void SetDateText(string value)
        {
            if (dateText == null)
                dateText = GetComponentInChildren<Text>(true);

            if (dateTmpText == null)
                dateTmpText = GetComponentInChildren<TMP_Text>(true);

            if (dateText != null)
                dateText.text = value;

            if (dateTmpText != null)
                dateTmpText.text = value;
        }
    }
}
