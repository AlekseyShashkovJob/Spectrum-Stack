using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

namespace GameCore.Hex
{
    public class HexGridManager : MonoBehaviour
    {
        [SerializeField] private GameObject _hexPrefab;
        [SerializeField] private Transform _hexesParent;
        [SerializeField] private int _initialPoolSize = 20;

        private readonly Dictionary<Vector2Int, HexButtonScript> _hexButtonDict = new();
        private readonly Stack<GameObject> _hexPool = new();

        private HexButtonToggler _buttonToggler;
        private Action _onVictory;

        private void Start()
        {
            _buttonToggler = new HexButtonToggler(_hexButtonDict);
            PrewarmPool();
        }

        private void PrewarmPool()
        {
            for (int i = 0; i < _initialPoolSize; i++)
            {
                GameObject go = Instantiate(_hexPrefab, _hexesParent);
                go.SetActive(false);
                _hexPool.Push(go);
            }
        }

        private GameObject GetHexFromPool()
        {
            if (_hexPool.Count > 0)
            {
                var go = _hexPool.Pop();
                go.SetActive(true);
                return go;
            }

            return Instantiate(_hexPrefab, _hexesParent);
        }

        private void ReturnHexToPool(GameObject go)
        {
            go.SetActive(false);
            go.transform.SetParent(_hexesParent);
            _hexPool.Push(go);
        }

        public void Init(Action onVictory)
        {
            _onVictory = onVictory;
        }

        public void OnHexPressed(HexButtonScript hex)
        {
            _buttonToggler.ToggleButtonAndNeighbours(hex);
            CheckVictory();
        }

        private void CheckVictory()
        {
            if (_hexButtonDict.Values.All(h => !h.IsOn))
            {
                _onVictory?.Invoke();
            }
        }

        public void GenerateGrid(List<HexData> hexes)
        {
            foreach (Transform child in _hexesParent)
            {
                ReturnHexToPool(child.gameObject);
            }

            _hexButtonDict.Clear();

            float hexWidth = _hexPrefab.GetComponent<RectTransform>().sizeDelta.x;
            float hexHeight = _hexPrefab.GetComponent<RectTransform>().sizeDelta.y;

            float verticalSpacing = hexHeight * 0.529f;
            float horizontalSpacing = hexWidth * 1.570f;

            var rows = hexes.GroupBy(h => h.row).OrderBy(g => g.Key);

            foreach (var rowGroup in rows)
            {
                CreateRow(rowGroup, verticalSpacing, horizontalSpacing);
            }
        }

        private void CreateRow(IGrouping<int, HexData> rowGroup, float verticalSpacing, float horizontalSpacing)
        {
            int rowIndex = rowGroup.Key;
            var rowList = rowGroup.OrderBy(h => h.colInRow).ToList();
            int countInRow = rowList.Count;

            float totalWidth = (countInRow - 1) * horizontalSpacing;
            float startX = -totalWidth / 2f;

            for (int i = 0; i < countInRow; i++)
            {
                var hex = rowList[i];
                CreateHex(hex, rowIndex, startX + i * horizontalSpacing, -rowIndex * verticalSpacing);
            }
        }

        private void CreateHex(HexData hex, int rowIndex, float offsetX, float offsetY)
        {
            GameObject hexGO = GetHexFromPool();
            hexGO.transform.SetParent(_hexesParent);
            RectTransform rt = hexGO.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(offsetX, offsetY);

            HexButtonScript script = hexGO.GetComponent<HexButtonScript>();
            if (script == null)
            {
                Debug.LogError("HexButtonScript not found on prefab.");
                return;
            }

            script.Setup(hex.colInRow, rowIndex, hex.type, hex.isOn, hex.spriteOn, hex.spriteOff, this);

            Vector2Int coord = new(hex.colInRow, rowIndex);
            _hexButtonDict[coord] = script;
        }
    }
}