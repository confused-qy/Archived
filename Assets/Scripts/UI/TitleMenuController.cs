using UnityEngine;
using UnityEngine.UI;

namespace EmployeeHandbook.DailyTasks
{
    /// <summary>
    /// HomePage 标题页按钮入口：新游戏、继续游戏、设置、退出。
    /// </summary>
    public class TitleMenuController : MonoBehaviour
    {
        [SerializeField] private string mainSceneName = "MainPage";
        [SerializeField] private Button continueButton;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private HomePageIntroController introController;

        private void Awake()
        {
            RefreshContinueButton();

            if (settingsPanel != null)
                settingsPanel.SetActive(false);
        }

        private void OnEnable()
        {
            RefreshContinueButton();
        }

        public void StartNewGame()
        {
            if (introController != null)
            {
                introController.PlayNewGameIntro();
                return;
            }

            StartNewGameWithoutIntro();
        }

        public void StartNewGameWithoutIntro()
        {
            GameLaunchState.RequestNewGame();
            LoadMainScene();
        }

        public void ContinueGame()
        {
            if (!SaveManager.HasSave())
            {
                Debug.Log("没有存档，无法继续游戏。");
                RefreshContinueButton();
                return;
            }

            GameLaunchState.RequestContinueGame();
            LoadMainScene();
        }

        public void OpenSettings()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(true);
        }

        public void CloseSettings()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(false);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void RefreshContinueButton()
        {
            if (continueButton != null)
                continueButton.interactable = SaveManager.HasSave();
        }

        private void LoadMainScene()
        {
            if (string.IsNullOrWhiteSpace(mainSceneName))
            {
                Debug.LogWarning("TitleMenuController 没有设置 Main Scene Name。", this);
                return;
            }

            SceneTransitionController.LoadScene(mainSceneName);
        }
    }
}
