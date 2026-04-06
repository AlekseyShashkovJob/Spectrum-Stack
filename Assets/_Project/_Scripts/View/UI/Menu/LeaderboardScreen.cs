using System;
using UnityEngine;
using View.Button;

namespace View.UI.Menu
{
    public class LeaderboardScreen : UIScreen
    {
        [SerializeField] private CustomButton _back;

        private void OnEnable()
        {
            _back.AddListener(BackToMenu);
        }

        private void OnDisable()
        {
            _back.RemoveListener(BackToMenu);
        }

        private void BackToMenu()
        {
            CloseScreen();
        }
    }
}