using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using View.Button;

namespace View.UI.Menu
{
    public class PrivacyScreen : UIScreen
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