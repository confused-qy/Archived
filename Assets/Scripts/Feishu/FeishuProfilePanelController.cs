using UnityEngine;

namespace EmployeeHandbook.Feishu
{
    public class FeishuProfilePanelController : MonoBehaviour
    {
        [SerializeField] private GameObject[] profilePanels;
        [SerializeField] private bool closeAllOnEnable = true;

        private void OnEnable()
        {
            if (closeAllOnEnable)
                CloseAllProfiles();
        }

        public void ShowProfile(GameObject profilePanel)
        {
            if (profilePanel == null)
            {
                Debug.LogWarning("FeishuProfilePanelController 收到空 Profile Panel。", this);
                return;
            }

            FeishuSfxPlayer.PlayOpenFriendProfileSfx();
            CloseAllProfiles();
            profilePanel.SetActive(true);
        }

        public void CloseProfile(GameObject profilePanel)
        {
            if (profilePanel != null)
                profilePanel.SetActive(false);
        }

        public void CloseProfileWithSound(GameObject profilePanel)
        {
            FeishuSfxPlayer.PlayCloseClickSfx();
            CloseProfile(profilePanel);
        }

        public void CloseAllProfiles()
        {
            if (profilePanels == null)
                return;

            for (int i = 0; i < profilePanels.Length; i++)
            {
                if (profilePanels[i] != null)
                    profilePanels[i].SetActive(false);
            }
        }

        public void CloseAllProfilesWithSound()
        {
            FeishuSfxPlayer.PlayCloseClickSfx();
            CloseAllProfiles();
        }
    }
}
