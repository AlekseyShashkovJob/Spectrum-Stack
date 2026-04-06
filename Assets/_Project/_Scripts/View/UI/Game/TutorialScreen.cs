using UnityEngine;
using View.Button;

namespace View.UI.Game
{
    public class TutorialScreen : UIScreen
    {
        [SerializeField] private CustomButton _start;

        private void OnEnable()
        {
            _start.AddListener(StartGame);
        }

        private void OnDisable()
        {
            _start.RemoveListener(StartGame);
        }

        public override void StartScreen()
        {
            base.StartScreen();

        }

        private void StartGame()
        {
            GameCore.GameManager.Instance.StartGameFromTutorial();
        }
    }
}