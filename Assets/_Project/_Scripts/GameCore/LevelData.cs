using System.Collections.Generic;
using UnityEngine;
using GameCore.Hex;

[CreateAssetMenu(fileName = "Level_", menuName = "HexGame/Level")]
public class LevelData : ScriptableObject
{
    public List<HexData> hexes;

}