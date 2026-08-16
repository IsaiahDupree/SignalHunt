using SignalHunt.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SignalHunt.UI
{
    public sealed class HoldControlButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private VehicleControl _control;

        public void Initialize(VehicleControl control)
        {
            _control = control;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            VehicleInputState.Set(_control, true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            VehicleInputState.Set(_control, false);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            VehicleInputState.Set(_control, false);
        }

        private void OnDisable()
        {
            VehicleInputState.Set(_control, false);
        }
    }
}
