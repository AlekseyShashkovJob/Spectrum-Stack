using UnityEngine;
using UnityEngine.UI;

namespace GameCore.Hex
{
    public class HexButtonScript : MonoBehaviour
    {
        public int X { get; private set; }
        public int Y { get; private set; }
        public HexType HexType { get; private set; }
        public bool IsOn { get; private set; }

        private HexGridManager _hexGridManager;

        private Image _selfImage;
        private Sprite _spriteOn;
        private Sprite _spriteOff;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _selfImage = GetComponent<Image>();
        }

        public void Setup(int x, int y, HexType type, bool isOn, Sprite onSprite, Sprite offSprite, HexGridManager mgr)
        {
            X = x;
            Y = y;
            HexType = type;
            IsOn = isOn;
            _spriteOn = onSprite;
            _spriteOff = offSprite;
            _hexGridManager = mgr;

            if (_button == null)
                _button = GetComponent<Button>();

            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => _hexGridManager.OnHexPressed(this));

            UpdateVisual();
        }

        public void Toggle()
        {
            IsOn = !IsOn;
            UpdateVisual();
            Misc.Services.VibroManager.Vibrate();
        }

        private void UpdateVisual()
        {
            _selfImage.sprite = IsOn ? _spriteOn : _spriteOff;
        }
    }
}