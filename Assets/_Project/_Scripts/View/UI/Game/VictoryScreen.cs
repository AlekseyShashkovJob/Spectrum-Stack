using UnityEngine;
using TMPro;
using View.Button;
using System;

namespace View.UI.Game
{
    public class VictoryScreen : UIScreen
    {
        [SerializeField] private CustomButton _loadNextButton;
        [SerializeField] private CustomButton _restart;
        [SerializeField] private TMP_Text _currentScoreText;
        [SerializeField] private TMP_Text _totalScoreText;
        [SerializeField] private TMP_Text _buttonText;

        private void OnEnable()
        {
            _loadNextButton.AddListener(OnLoadNextClicked);
            _restart.AddListener(Restart);

            UpdateNextButtonText();
        }

        private void OnDisable()
        {
            _loadNextButton.RemoveListener(OnLoadNextClicked);
            _restart.RemoveListener(Restart);
        }

        public override void StartScreen()
        {
            base.StartScreen();

            var gm = GameCore.GameManager.Instance;
            _currentScoreText.text = $"SCORE {gm.CurrentScore}";
            _totalScoreText.text = $"BEST {gm.TotalScore}";
        }

        private void UpdateNextButtonText()
        {
            var gameManager = GameCore.GameManager.Instance;
            var levelLoader = gameManager != null ? gameManager.GetLevelLoader() : null;
            if (levelLoader == null)
                return;

            if (_buttonText == null)
                return;

            if (levelLoader.HasNextLevel())
                _buttonText.text = "LOAD NEXT";
            else
                _buttonText.text = "BACK TO MENU";
        }

        private void OnLoadNextClicked()
        {
            var gameManager = GameCore.GameManager.Instance;
            var levelLoader = gameManager.GetLevelLoader();

            if (levelLoader.HasNextLevel())
                gameManager.OnNextLevel();
            else
                gameManager.FinishGame();

            CloseScreen();
        }

        private void Restart()
        {
            GameCore.GameManager.Instance.Restart();
            CloseScreen();
        }
    }
}