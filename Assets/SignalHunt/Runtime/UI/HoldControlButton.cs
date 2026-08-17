using SignalHunt.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SignalHunt.UI
{
    public sealed class HoldControlButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private VehicleControl _control;
        private Image _image;
        private Color _baseColor;

        public void Initialize(VehicleControl control, Image image, Color baseColor)
        {
            _control = control;
            _image = image;
            _baseColor = baseColor;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            VehicleInputState.Set(_control, true);
            SetPressed(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            VehicleInputState.Set(_control, false);
            SetPressed(false);
        }

        private void OnDisable()
        {
            VehicleInputState.Set(_control, false);
            SetPressed(false);
        }

        private void SetPressed(bool pressed)
        {
            if (_image == null)
            {
                return;
            }
            _image.color = pressed
                ? new Color(_baseColor.r, _baseColor.g, _baseColor.b, 0.82f)
                : new Color(_baseColor.r, _baseColor.g, _baseColor.b, 0.34f);
            transform.localScale = pressed ? Vector3.one * 0.96f : Vector3.one;
        }
    }
}
