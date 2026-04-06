using UnityEngine;

namespace GameCore.Hex
{
    [System.Serializable]
    public class HexData
    {
        public int row;
        public int colInRow;
        public HexType type;
        public bool isOn;
        public Sprite spriteOn;
        public Sprite spriteOff;
    }
}