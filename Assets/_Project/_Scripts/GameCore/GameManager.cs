using TMPro;
using UnityEngine;
using System.Collections;

namespace GameCore
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        [SerializeField] private LevelLoader levelLoader;
        [SerializeField] private View.UI.UIScreen _winScreen;
        [SerializeField] private View.UI.UIScreen _loseScreen;
        [SerializeField] private View.UI.UIScreen _tutorialScreen;
        [SerializeField] private Misc.SceneManagment.SceneLoader _sceneLoader;
        [SerializeField] private TMP_Text _timerText;

        public int CurrentScore { get; private set; } = 0;
        public int TotalScore { get; private set; } = 0;

        private const float MAX_TIME = 40.0f;
        private Coroutine _timerCoroutine;
        private float _timeLeft;

        private int _currentLevelIndex => levelLoader.CurrentLevel;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                LoadData();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            int selectedLevel = PlayerPrefs.GetInt(GameConstants.LAST_SELECTED_LEVEL_KEY, 0);
            StartLevel(selectedLevel);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public LevelLoader GetLevelLoader() => levelLoader;

        public void StartLevel(int levelIndex)
        {
            CurrentScore = 0;
            _timeLeft = MAX_TIME;
            UpdateTimerUI();

            levelLoader.LoadLevel(levelIndex);

            if (levelIndex == 0)
            {
                _tutorialScreen.StartScreen();
            }
            else
            {
                StartLevelTimer();
            }
        }

        public void StartGameFromTutorial()
        {
            _tutorialScreen.CloseScreen();
            StartLevelTimer();
        }

        public void StartLevelTimer()
        {
            if (_timerCoroutine != null)
                StopCoroutine(_timerCoroutine);

            _timerCoroutine = StartCoroutine(TimerRoutine());
        }

        public void OnLevelCompleted()
        {
            int basePoints = (_currentLevelIndex + 1) * 5;
            int timeBonus = Mathf.CeilToInt(_timeLeft);
            int score = basePoints + timeBonus;

            CurrentScore = score;

            SaveBestScore(_currentLevelIndex, score);
            UnlockNextLevel(_currentLevelIndex);

            StopTimer();
            _winScreen.StartScreen();
        }

        public void OnNextLevel()
        {
            int next = _currentLevelIndex + 1;
            int totalLevels = levelLoader.TotalLevels();

            if (next >= totalLevels)
            {
                FinishGame();
            }
            else
            {
                StartLevel(next);
            }
        }

        public void Restart()
        {
            int current = _currentLevelIndex;
            StartLevel(current);
        }

        public void FinishGame()
        {
            StopTimer();
            SaveCurrentTotalScore();
            _sceneLoader.ChangeScene(Misc.Data.SceneConstants.MENU_SCENE);
        }

        private IEnumerator TimerRoutine()
        {
            while (_timeLeft > 0)
            {
                _timeLeft -= Time.deltaTime;
                UpdateTimerUI();
                yield return null;
            }

            _timeLeft = 0;
            UpdateTimerUI();
            OnTimeOver();
        }

        private void UpdateTimerUI()
        {
            int seconds = Mathf.CeilToInt(_timeLeft);
            _timerText.text = $"{seconds}";
        }

        private void OnTimeOver()
        {
            StopTimer();
            CurrentScore = 0;
            SaveCurrentTotalScore();
            _loseScreen.StartScreen();
        }

        private void StopTimer()
        {
            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }
        }

        private void SaveBestScore(int levelIndex, int newScore)
        {
            string key = $"{GameConstants.LEVEL_BEST_SCORE_KEY}_{levelIndex}";
            int saved = PlayerPrefs.GetInt(key, 0);

            if (newScore > saved)
            {
                PlayerPrefs.SetInt(key, newScore);
                PlayerPrefs.Save();
            }

            TotalScore = Mathf.Max(TotalScore, newScore);
        }

        private void UnlockNextLevel(int currentLevel)
        {
            int lastUnlocked = PlayerPrefs.GetInt(GameConstants.LAST_UNLOCKED_LEVEL_KEY, 0);
            if (currentLevel >= lastUnlocked)
            {
                PlayerPrefs.SetInt(GameConstants.LAST_UNLOCKED_LEVEL_KEY, currentLevel + 1);
                PlayerPrefs.Save();
            }
        }

        private void SaveCurrentTotalScore()
        {
            string key = $"{GameConstants.LEVEL_BEST_SCORE_KEY}_{_currentLevelIndex}";
            int saved = PlayerPrefs.GetInt(key, 0);

            if (CurrentScore > saved)
            {
                PlayerPrefs.SetInt(key, CurrentScore);
                PlayerPrefs.Save();
                TotalScore = CurrentScore;
            }
            else
            {
                TotalScore = saved;
            }
        }

        private void LoadData()
        {
            int lastSelected = PlayerPrefs.GetInt(GameConstants.LAST_SELECTED_LEVEL_KEY, 0);
            string key = $"{GameConstants.LEVEL_BEST_SCORE_KEY}_{lastSelected}";
            TotalScore = PlayerPrefs.GetInt(key, 0);
        }
    }
}

