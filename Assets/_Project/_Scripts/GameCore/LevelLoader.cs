using UnityEngine;
using GameCore.Hex;

namespace GameCore
{
    public class LevelLoader : MonoBehaviour
    {
        [SerializeField] private LevelData[] _levelDataAssets;
        [SerializeField] private HexGridManager _hexGridManager;

        public int CurrentLevel { get; private set; } = 0;

        public void LoadLevel(int index)
        {
            if (index < 0 || index >= _levelDataAssets.Length)
            {
                Debug.LogError("Level index out of range");
                return;
            }

            CurrentLevel = index;
            LevelData levelData = _levelDataAssets[index];

            _hexGridManager.Init(GameManager.Instance.OnLevelCompleted);
            _hexGridManager.GenerateGrid(levelData.hexes);
        }

        public void LoadNextLevel()
        {
            int next = CurrentLevel + 1;
            if (next >= _levelDataAssets.Length)
            {
                GameManager.Instance.FinishGame();
                Debug.Log("All levels complete!");
            }
            else
            {
                LoadLevel(next);
            }
        }

        public bool HasNextLevel()
        {
            return (CurrentLevel + 1) < _levelDataAssets.Length;
        }

        public int TotalLevels()
        {
            return _levelDataAssets.Length;
        }
    }
}