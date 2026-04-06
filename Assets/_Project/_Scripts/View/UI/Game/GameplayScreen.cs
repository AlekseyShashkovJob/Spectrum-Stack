using UnityEngine;
using System;
using View.Button;

namespace View.UI.Game
{
    public class GameplayScreen : UIScreen
    {
        [SerializeField] private CustomButton _pause;
        [SerializeField] private UIScreen _pauseScreen;

        private bool _isPaused = false;

        private void OnEnable()
        {
            _pause.AddListener(TogglePause);
        }

        private void OnDisable()
        {
            _pause.RemoveListener(TogglePause);
        }

        private void TogglePause()
        {
            _isPaused = !_isPaused;
            Time.timeScale = _isPaused ? 0.0f : 1.0f;
            _pauseScreen.StartScreen();
        }
    }
}