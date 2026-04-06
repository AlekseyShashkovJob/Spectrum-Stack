using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameCore.Hex
{
    public class HexButtonToggler
    {
        private readonly Dictionary<Vector2Int, HexButtonScript> _hexButtonDict;

        public HexButtonToggler(Dictionary<Vector2Int, HexButtonScript> hexButtonDict)
        {
            _hexButtonDict = hexButtonDict;
        }

        public void ToggleButtonAndNeighbours(HexButtonScript hex)
        {
            var affectedHexes = GetAffectedHexes(hex);
            affectedHexes.ForEach(h => h.Toggle());
            hex.Toggle();
        }

        private List<HexButtonScript> GetAffectedHexes(HexButtonScript hex)
        {
            var hexActions = new Dictionary<HexType, Func<IEnumerable<HexButtonScript>>>
            {
                { HexType.Empty, () => new List<HexButtonScript> { hex } },
                { HexType.ToggleSelf, () => Enumerable.Empty<HexButtonScript>() },
                { HexType.ToggleSelfAndDiagonals, () => GetDiagonals(hex.X, hex.Y) },
                { HexType.ToggleSelfAndTriangle, () => GetTriangleAffected(hex.X, hex.Y) },
                { HexType.ToggleSelfAndNeighbours, () => GetAllNeighbors(hex.X, hex.Y) }
            };

            return hexActions.TryGetValue(hex.HexType, out var action) ? action().ToList() : new List<HexButtonScript>();
        }

        private List<HexButtonScript> GetDiagonals(int x, int y)
        {
            var diagonals = new List<(int dx, int dy)>();
            int columnCount = GetColumnCount(y);

            if (columnCount == 1)
            {
                diagonals.Add((0, -1));
                diagonals.Add((1, -1));
                diagonals.Add((0, 1));
                diagonals.Add((1, 1));
            }
            else if (columnCount == 2)
            {
                diagonals.Add((0, -1));
                diagonals.Add((1, -1));
                diagonals.Add((0, 1));
                diagonals.Add((1, 1));

                int topRowCount = GetColumnCount(y - 1);
                if (topRowCount < 3 && _hexButtonDict.ContainsKey(new Vector2Int(x - 1, y - 1)))
                    diagonals.Add((-1, -1));

                int bottomRowCount = GetColumnCount(y + 1);
                if (bottomRowCount < 3 && _hexButtonDict.ContainsKey(new Vector2Int(x - 1, y + 1)))
                    diagonals.Add((-1, 1));
            }
            else if (columnCount == 3)
            {
                if (x == 0)
                {
                    diagonals.Add((0, -1));
                    diagonals.Add((0, 1));
                }
                else if (x == 1)
                {
                    diagonals.Add((-1, -1));
                    diagonals.Add((0, -1));
                    diagonals.Add((-1, 1));
                    diagonals.Add((0, 1));
                }
                else if (x == 2)
                {
                    diagonals.Add((-1, -1));
                    diagonals.Add((-1, 1));
                }
            }

            return GetHexButtonsFromOffsets(x, y, diagonals);
        }

        private List<HexButtonScript> GetTriangleAffected(int x, int y)
        {
            var directions = new List<(int dx, int dy)>();
            int columnCount = GetColumnCount(y);

            if (columnCount == 1)
                directions.AddRange(GetSingleColumnTriangleOffsets(x, y));
            else if (columnCount == 2)
                directions.AddRange(GetTwoColumnTriangleOffsets(x, y));
            else if (columnCount == 3)
                directions.AddRange(GetThreeColumnTriangleOffsets(x, y));

            var topHex = GetTopHexOffsetIfExists(x, y);
            if (topHex.HasValue)
                directions.Add(topHex.Value);

            return GetHexButtonsFromOffsets(x, y, directions);
        }

        private (int dx, int dy)? GetTopHexOffsetIfExists(int x, int y)
        {
            int columnCount = GetColumnCount(y);

            if (columnCount == 1)
            {
                if (_hexButtonDict.ContainsKey(new Vector2Int(x + 1, y - 2)))
                    return (1, -2);
                else if (_hexButtonDict.ContainsKey(new Vector2Int(x, y - 2)))
                    return (0, -2);

            }
            else if (columnCount == 2)
            {
                if (_hexButtonDict.ContainsKey(new Vector2Int(x, y - 2)))
                    return (0, -2);
                else if (_hexButtonDict.ContainsKey(new Vector2Int(x - 1, y - 2)))
                    return (-1, -2);
            }
            else if (columnCount == 3)
            {
                if (x == 0)
                {
                    if (_hexButtonDict.ContainsKey(new Vector2Int(x - 1, y - 2)))
                        return (-1, -2);
                    else if (_hexButtonDict.ContainsKey(new Vector2Int(x, y - 2)))
                        return (0, -2);
                }
                else if (x == 1)
                {
                    if (_hexButtonDict.ContainsKey(new Vector2Int(x - 1, y - 2)))
                        return (-1, -2);
                    else if (_hexButtonDict.ContainsKey(new Vector2Int(x, y - 2)))
                        return (0, -2);
                    else if (_hexButtonDict.ContainsKey(new Vector2Int(x + 1, y - 2)))
                        return (1, -2);
                }
                else if (x == 2)
                {
                    if (_hexButtonDict.ContainsKey(new Vector2Int(x + 1, y - 2)))
                        return (1, -2);
                    else if (_hexButtonDict.ContainsKey(new Vector2Int(x, y - 2)))
                        return (0, -2);
                }
            }

            return null;
        }

        private IEnumerable<(int dx, int dy)> GetSingleColumnTriangleOffsets(int x, int y)
        {
            var offsets = new List<(int dx, int dy)>
            {
                (0, 1), (1, 1)
            };

            return offsets.Where(offset => _hexButtonDict.ContainsKey(new Vector2Int(x + offset.dx, y + offset.dy)));
        }

        private IEnumerable<(int dx, int dy)> GetTwoColumnTriangleOffsets(int x, int y)
        {
            var offsets = new List<(int dx, int dy)>
            {
                (0, -2), (0, 1), (1, 1)
            };

            if (_hexButtonDict.ContainsKey(new Vector2Int(x, y - 2)))
            {
                offsets.Add((0, -2));
            }

            int bottomRowCount = GetColumnCount(y + 1);
            if (bottomRowCount < 3 && _hexButtonDict.ContainsKey(new Vector2Int(x - 1, y + 1)))
            {
                offsets.Add((-1, 1));
            }

            return offsets.Where(offset => _hexButtonDict.ContainsKey(new Vector2Int(x + offset.dx, y + offset.dy)));
        }

        private IEnumerable<(int dx, int dy)> GetThreeColumnTriangleOffsets(int x, int y)
        {
            var offsets = new List<(int dx, int dy)>();

            if (x == 0)
            {
                if (IsTopLeft(y) && _hexButtonDict.ContainsKey(new Vector2Int(x - 1, y - 2)))
                    offsets.Add((-1, -2));
                else if (IsTopMiddle(y) && _hexButtonDict.ContainsKey(new Vector2Int(x, y - 2)))
                    offsets.Add((0, -2));

                if (_hexButtonDict.ContainsKey(new Vector2Int(x + 0, y + 1)))
                    offsets.Add((0, 1));
            }
            else if (x == 1)
            {
                if (IsTopLeft(y) && _hexButtonDict.ContainsKey(new Vector2Int(x - 1, y - 2)))
                    offsets.Add((-1, -2));
                else if (IsTopMiddle(y) && _hexButtonDict.ContainsKey(new Vector2Int(x, y - 2)))
                    offsets.Add((0, -2));

                if (_hexButtonDict.ContainsKey(new Vector2Int(x - 1, y + 1)))
                    offsets.Add((-1, 1));
                if (_hexButtonDict.ContainsKey(new Vector2Int(x + 0, y + 1)))
                    offsets.Add((0, 1));
            }
            else if (x == 2)
            {
                if (IsTopLeft(y) && _hexButtonDict.ContainsKey(new Vector2Int(x - 1, y - 2)))
                    offsets.Add((-1, -2));
                else if (IsTopMiddle(y) && _hexButtonDict.ContainsKey(new Vector2Int(x, y - 2)))
                    offsets.Add((0, -2));

                if (_hexButtonDict.ContainsKey(new Vector2Int(x - 1, y + 1)))
                    offsets.Add((-1, 1));
            }

            return offsets;
        }

        private List<HexButtonScript> GetAllNeighbors(int x, int y)
        {
            int columnCount = GetColumnCount(y);

            var directions = columnCount switch
            {
                1 => GetOneColumnNeighbors(x, y),
                2 => GetTwoColumnNeighbors(x, y),
                3 => GetThreeColumnNeighbors(x, y),
                _ => new List<(int dx, int dy)>()
            };

            return GetHexButtonsFromOffsets(x, y, directions);
        }

        private List<(int dx, int dy)> GetOneColumnNeighbors(int x, int y)
        {
            var directions = new List<(int dx, int dy)>
            {
                (0, 1), (1, 1),
                (0, -1), (1, -1)
            };

            if (IsTopLeft(y)) directions.Add((0, -2));
            else if (IsTopMiddle(y)) directions.Add((1, -2));

            if (IsBotLeft(y)) directions.Add((0, 2));
            else if (IsBotMiddle(y)) directions.Add((1, 2));

            return directions;
        }

        private List<(int dx, int dy)> GetTwoColumnNeighbors(int x, int y)
        {
            var directions = new List<(int dx, int dy)>
            {
                (0, 1), (1, 1),
                (0, -1), (1, -1),
                (0, -2), (0, 2)
            };

            if (GetColumnCount(y - 1) < 3) directions.Add((-1, -1));
            if (GetColumnCount(y + 1) < 3) directions.Add((-1, 1));

            return directions;
        }

        private List<(int dx, int dy)> GetThreeColumnNeighbors(int x, int y)
        {
            var directions = new List<(int dx, int dy)>();

            bool isTopMid = IsTopMiddle(y);
            bool isTopLeft = IsTopLeft(y);
            bool isBotMid = IsBotMiddle(y);
            bool isBotLeft = IsBotLeft(y);

            if (x == 0)
            {
                if (isTopMid) directions.Add((0, -2));
                else if (isTopLeft) directions.Add((-1, -2));

                directions.Add((0, 1));
                directions.Add((0, -1));

                if (isBotMid) directions.Add((0, 2));
                else if (isBotLeft) directions.Add((-1, 2));
            }
            else if (x == 1)
            {
                if (isTopMid) directions.Add((0, -2));
                else if (isTopLeft) directions.Add((-1, -2));

                directions.Add((0, 1));
                directions.Add((0, -1));
                directions.Add((-1, 1));
                directions.Add((-1, -1));

                if (isBotMid) directions.Add((0, 2));
                else if (isBotLeft) directions.Add((-1, 2));
            }
            else if (x == 2)
            {
                if (isTopMid) directions.Add((0, -2));
                else if (isTopLeft) directions.Add((-1, -2));

                directions.Add((0, 1));
                directions.Add((0, -1));
                directions.Add((-1, 1));
                directions.Add((-1, -1));

                if (isBotMid) directions.Add((0, 2));
                else if (isBotLeft) directions.Add((-1, 2));
            }

            return directions;
        }

        private bool IsTopMiddle(int y)
        {
            int a1 = GetColumnCount(y - 1);
            int a2 = GetColumnCount(y - 2);
            return (a1 == 2 && a2 == 3) || (a1 == 3 && a2 == 2);
        }

        private bool IsTopLeft(int y)
        {
            int a1 = GetColumnCount(y - 1);
            int a2 = GetColumnCount(y - 2);
            return (a1 == 2 && a2 == 1) || (a1 == 1 && a2 == 2);
        }

        private bool IsBotMiddle(int y)
        {
            int b1 = GetColumnCount(y + 1);
            int b2 = GetColumnCount(y + 2);
            return (b1 == 2 && b2 == 3) || (b1 == 3 && b2 == 2);
        }

        private bool IsBotLeft(int y)
        {
            int b1 = GetColumnCount(y + 1);
            int b2 = GetColumnCount(y + 2);
            return (b1 == 2 && b2 == 1) || (b1 == 1 && b2 == 2);
        }

        private List<HexButtonScript> GetHexButtonsFromOffsets(int x, int y, List<(int dx, int dy)> offsets)
        {
            return offsets
                .Select(offset => new Vector2Int(x + offset.dx, y + offset.dy))
                .Where(pos => _hexButtonDict.ContainsKey(pos))
                .Select(pos => _hexButtonDict[pos])
                .ToList();
        }

        private int GetColumnCount(int rowIndex)
        {
            return _hexButtonDict.Count(kvp => kvp.Key.y == rowIndex);
        }
    }
}