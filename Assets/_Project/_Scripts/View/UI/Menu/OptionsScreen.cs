using System;
using UnityEngine;
using View.Button;
using UnityEngine.UI;

namespace View.UI.Menu
{
    public class OptionsScreen : UIScreen
    {
        [SerializeField] private CustomButton _back;
        [SerializeField] private CustomButton _sound;
        [SerializeField] private CustomButton _vibro;
        [SerializeField] private Image _soundImage;
        [SerializeField] private Image _vibroImage;
        [SerializeField] private Sprite _spriteOn;
        [SerializeField] private Sprite _spriteOff;

        private bool _isSoundOn;
        private bool _isVibroOn;

        public static Action OnSoundStateChanged;
        public static Action OnVibroStateChanged;

        private void OnEnable()
        {
            _back.AddListener(BackToMenu);
            _sound.AddListener(ToggleSoundImage);
            _vibro.AddListener(ToggleVibroImage);

            _isSoundOn = PlayerPrefs.GetInt(Misc.Services.PlayerPrefsKeys.SoundOn, 1) == 1;
            _isVibroOn = PlayerPrefs.GetInt(Misc.Services.PlayerPrefsKeys.VibroOn, 0) == 1;

            UpdateSoundUI();
            UpdateVibroUI();
        }

        private void OnDisable()
        {
            _back.RemoveListener(BackToMenu);
            _sound.RemoveListener(ToggleSoundImage);
            _vibro.RemoveListener(ToggleVibroImage);
        }

        private void BackToMenu()
        {
            CloseScreen();
        }

        private void ToggleSoundImage()
        {
            _isSoundOn = !_isSoundOn;
            PlayerPrefs.SetInt(Misc.Services.PlayerPrefsKeys.SoundOn, _isSoundOn ? 1 : 0);
            UpdateSoundUI();
            OnSoundStateChanged?.Invoke();
        }

        private void ToggleVibroImage()
        {
            _isVibroOn = !_isVibroOn;
            PlayerPrefs.SetInt(Misc.Services.PlayerPrefsKeys.VibroOn, _isVibroOn ? 1 : 0);
            UpdateVibroUI();
            OnVibroStateChanged?.Invoke();
        }

        private void UpdateSoundUI()
        {
            _soundImage.sprite = _isSoundOn ? _spriteOn : _spriteOff;
        }

        private void UpdateVibroUI()
        {
            _vibroImage.sprite = _isVibroOn ? _spriteOn : _spriteOff;
        }
    }
}