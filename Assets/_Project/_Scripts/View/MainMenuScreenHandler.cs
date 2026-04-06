using System.Collections;
using UnityEngine;

namespace View.UI
{
    public class MainMenuScreenHandler : MonoBehaviour
    {
        public enum AppMode { None, WebView, Game }

        [SerializeField] private UIScreen _gameMenu;
        [SerializeField] private UIScreen _internet;
        [SerializeField] private GameObject _loading;

        private bool _allowOrientationChange = true;
        private AppMode _mode;

        private void Awake()
        {
            _mode = (AppMode)PlayerPrefs.GetInt(
                Misc.Data.ServicesConstants.WEB_VIEW_MODE_KEY, (int)AppMode.None);
        }

        private void Start()
        {
            StartCoroutine(ProceedAppFlow());
        }

        private void Update()
        {
            if (_allowOrientationChange)
            {
                HandleOrientationChange();
            }
        }

        private IEnumerator ProceedAppFlow()
        {
            _loading.SetActive(true);

            if (_mode == AppMode.None)
            {
                yield return StartCoroutine(FirstLaunch());
            }
            else
            {
                _loading.SetActive(false);
                StopOrientationChange();
                _gameMenu.StartScreen();
            }
        }

        private IEnumerator FirstLaunch()
        {
            bool isAvailable = false;

            yield return StartCoroutine(
                new Misc.Services.InternetChecker()
                    .CheckAvailability(result => isAvailable = result));

            if (!isAvailable)
            {
                _loading.SetActive(false);
                _internet.StartScreen();
                yield break;
            }

            _loading.SetActive(false);
            StopOrientationChange();
            _gameMenu.StartScreen();
        }

        private void HandleOrientationChange()
        {
            var currentOrientation = Input.deviceOrientation;

            if (currentOrientation == DeviceOrientation.LandscapeLeft
                && Screen.orientation != ScreenOrientation.LandscapeLeft)
                Screen.orientation = ScreenOrientation.LandscapeLeft;
            else if (currentOrientation == DeviceOrientation.LandscapeRight
                && Screen.orientation != ScreenOrientation.LandscapeRight)
                Screen.orientation = ScreenOrientation.LandscapeRight;
            else if (currentOrientation == DeviceOrientation.Portrait
                && Screen.orientation != ScreenOrientation.Portrait)
                Screen.orientation = ScreenOrientation.Portrait;
            else if (currentOrientation == DeviceOrientation.PortraitUpsideDown
                && Screen.orientation != ScreenOrientation.PortraitUpsideDown)
                Screen.orientation = ScreenOrientation.PortraitUpsideDown;
        }

        private void StopOrientationChange()
        {
            _allowOrientationChange = false;

            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToPortrait = false;
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        }
    }
}