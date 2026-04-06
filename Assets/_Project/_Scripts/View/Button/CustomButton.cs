using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace View.Button
{
    [RequireComponent(typeof(Image))]
    public abstract class CustomButton : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler
    {
        public event Action OnClick;

        private bool _isPointerDown;

        public void AddListener(Action listener) => OnClick += listener;
        public void RemoveListener(Action listener) => OnClick -= listener;

        public virtual void OnPointerDown(PointerEventData eventData)
        {
            _isPointerDown = true;
        }

        public virtual void OnPointerUp(PointerEventData eventData)
        {
            if (_isPointerDown)
            {
                _isPointerDown = false;
                OnClick?.Invoke();
            }
        }
    }
}