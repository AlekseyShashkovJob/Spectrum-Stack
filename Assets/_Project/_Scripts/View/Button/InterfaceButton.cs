using UnityEngine;
using UnityEngine.EventSystems;

namespace View.Button
{
    public class InterfaceButton : CustomButton
    {
        private const float AnimationModificator = 1.07f;

        [SerializeField] private AudioClip _clickSound;

        private Vector3 _startScale;

        private void OnEnable()
        {
            _startScale = transform.localScale;
        }

        private void OnDisable()
        {
            transform.localScale = _startScale;
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);

            Misc.Services.SoundManager.Instance.PlayClick(_clickSound);
            transform.localScale *= AnimationModificator;
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            transform.localScale = _startScale;
            base.OnPointerUp(eventData);
        }
    }
}