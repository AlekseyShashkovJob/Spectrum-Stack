using UnityEngine;

namespace View.UI.Game
{
    public class PausedScreen : UIScreen
    {
        [SerializeField] private UIScreen _optionsScreen;
        [SerializeField] private Button.CustomButton _continue;
        [SerializeField] private Button.CustomButton _settings;
        [SerializeField] private Button.CustomButton _menu;

        [SerializeField] private Misc.SceneManagment.SceneLoader _sceneLoader;

        private void OnEnable()
        {
            _continue.AddListener(ContinueGame);
            _settings.AddListener(OpenOptions);
            _menu.AddListener(BackToMenu);
        }

        private void OnDisable()
        {
            _continue.RemoveListener(ContinueGame);
            _settings.RemoveListener(OpenOptions);
            _menu.RemoveListener(BackToMenu);
        }

        public override void StartScreen()
        {
            base.StartScreen();

            Time.timeScale = 0.0f;
        }

        private void ContinueGame()
        {
            Time.timeScale = 1.0f;
            CloseScreen();
        }

        private void OpenOptions()
        {
            _optionsScreen.StartScreen();
        }

        private void BackToMenu()
        {
            Time.timeScale = 1.0f;
            _sceneLoader.ChangeScene(Misc.Data.SceneConstants.MENU_SCENE);
            CloseScreen();
        }
    }
}