using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SignalHunt.UI
{
    public sealed class TouchControlPad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private RectTransform _surface;
        private RectTransform _thumb;
        private Action<Vector2> _valueChanged;
        private bool _horizontalOnly;
        private float _deadZone;
        private int _activePointer = int.MinValue;

        public Vector2 Value { get; private set; }

        public void Initialize(RectTransform surface, RectTransform thumb, Action<Vector2> valueChanged,
            bool horizontalOnly = false, float deadZone = 0.08f)
        {
            _surface = surface;
            _thumb = thumb;
            _valueChanged = valueChanged;
            _horizontalOnly = horizontalOnly;
            _deadZone = Mathf.Clamp(deadZone, 0f, 0.45f);
            ResetValue();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_activePointer != int.MinValue)
            {
                return;
            }
            _activePointer = eventData.pointerId;
            UpdateValue(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == _activePointer)
            {
                UpdateValue(eventData);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _activePointer)
            {
                return;
            }
            _activePointer = int.MinValue;
            ResetValue();
        }

        private void OnDisable()
        {
            _activePointer = int.MinValue;
            ResetValue();
        }

        private void UpdateValue(PointerEventData eventData)
        {
            if (_surface == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _surface, eventData.position, eventData.pressEventCamera, out var localPoint))
            {
                return;
            }

            var rect = _surface.rect;
            var radius = Mathf.Max(1f, Mathf.Min(rect.width, rect.height) * 0.43f);
            var delta = localPoint - rect.center;
            var raw = _horizontalOnly
                ? new Vector2(Mathf.Clamp(delta.x / radius, -1f, 1f), 0f)
                : Vector2.ClampMagnitude(delta / radius, 1f);
            var magnitude = raw.magnitude;
            Value = magnitude <= _deadZone
                ? Vector2.zero
                : raw.normalized * Mathf.InverseLerp(_deadZone, 1f, magnitude);
            ApplyValue();
        }

        private void ResetValue()
        {
            Value = Vector2.zero;
            ApplyValue();
        }

        private void ApplyValue()
        {
            if (_thumb != null && _surface != null)
            {
                var radius = Mathf.Min(_surface.rect.width, _surface.rect.height) * 0.31f;
                _thumb.anchoredPosition = Value * radius;
                _thumb.localScale = Value.sqrMagnitude > 0.001f ? Vector3.one * 0.92f : Vector3.one;
            }
            _valueChanged?.Invoke(Value);
        }
    }
}
