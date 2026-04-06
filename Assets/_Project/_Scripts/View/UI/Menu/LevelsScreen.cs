using UnityEngine;
using UnityEngine.UI;
using View.UI;
using View.Button;
using Misc.SceneManagment;

namespace View.UI.Menu
{
    public class LevelsScreen : UIScreen
    {
        [SerializeField] private SceneLoader _sceneLoader;

        [Header("=== Страницы ===")]
        [SerializeField] private GameObject _page1;
        [SerializeField] private GameObject _page2;

        [Header("=== Навигация ===")]
        [SerializeField] private CustomButton _nextButton;
        [SerializeField] private CustomButton _prevButton;
        [SerializeField] private CustomButton _back;

        [Header("=== Уровни ===")]
        [SerializeField] private LevelButton[] _levelButtons;

        private void OnEnable()
        {
            _nextButton.AddListener(OpenPage2);
            _prevButton.AddListener(OpenPage1);
            _back.AddListener(BackToMenu);

            OpenPage1();
        }

        private void OnDisable()
        {
            _nextButton.RemoveListener(OpenPage2);
            _prevButton.RemoveListener(OpenPage1);
            _back.RemoveListener(BackToMenu);
        }

        private void Start()
        {
            InitLevels();
        }

        private void InitLevels()
        {
            int unlockedLevel = PlayerPrefs.GetInt(GameCore.GameConstants.LAST_UNLOCKED_LEVEL_KEY, 0);

            for (int i = 0; i < _levelButtons.Length; i++)
            {
                bool isUnlocked = i <= unlockedLevel;
                _levelButtons[i].Init(i, isUnlocked, OnLevelSelected);
            }
        }

        private void OnLevelSelected(int levelIndex)
        {
            PlayerPrefs.SetInt(GameCore.GameConstants.LAST_SELECTED_LEVEL_KEY, levelIndex);
            PlayerPrefs.Save();

            _sceneLoader.ChangeScene(Misc.Data.SceneConstants.GAME_SCENE);
            CloseScreen();
        }

        private void OpenPage1()
        {
            _page1.SetActive(true);
            _page2.SetActive(false);
        }

        private void OpenPage2()
        {
            _page1.SetActive(false);
            _page2.SetActive(true);
        }

        private void BackToMenu()
        {
            CloseScreen();
        }
    }

    [System.Serializable]
    public class LevelButton
    {
        [SerializeField] private CustomButton _button;
        [SerializeField] private Image _image;
        [SerializeField] private Sprite _onSprite;
        [SerializeField] private Sprite _offSprite;

        private int _index;
        private bool _isUnlocked;
        private System.Action<int> _onClick;

        public void Init(int index, bool isUnlocked, System.Action<int> onClick)
        {
            _index = index;
            _isUnlocked = isUnlocked;
            _onClick = onClick;

            _image.sprite = isUnlocked ? _onSprite : _offSprite;

            _button.RemoveListener(HandleClick);
            if (isUnlocked)
                _button.AddListener(HandleClick);
        }

        private void HandleClick()
        {
            _onClick?.Invoke(_index);
        }
    }
}