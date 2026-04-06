using UnityEngine;
using System;

namespace View.UI.Menu
{
    public class PushPermissionScreen : UIScreen
    {
        [SerializeField] private GameObject _backgroundPortrait;
        [SerializeField] private GameObject _backgroundLandscape;

        private Vector2Int _lastResolution;

        private void Update()
        {
            Vector2Int currentResolution = new Vector2Int(Screen.width, Screen.height);
            if (currentResolution != _lastResolution)
            {
                _lastResolution = currentResolution;
                UpdateBackground();
            }
        }

        public override void StartScreen()
        {
            base.StartScreen();

            _lastResolution = new Vector2Int(Screen.width, Screen.height);
            UpdateBackground();
        }

        private void UpdateBackground()
        {
            bool isPortrait = Screen.height >= Screen.width;

            _backgroundPortrait.SetActive(isPortrait);
            _backgroundLandscape.SetActive(!isPortrait);
        }
    }
}