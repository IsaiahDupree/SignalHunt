using System;
using UnityEngine;

namespace SignalHunt.World
{
    public sealed class RelicPickup : MonoBehaviour
    {
        private string _relicId;
        private Action<string> _onCollected;
        private Vector3 _initialPosition;
        private bool _collected;
        private Vector3 _baseScale;

        public void Initialize(string relicId, Action<string> onCollected)
        {
            _relicId = relicId;
            _onCollected = onCollected;
            _initialPosition = transform.position;
            _baseScale = transform.localScale;
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, 75f * Time.deltaTime, Space.World);
            transform.position = _initialPosition + Vector3.up * (Mathf.Sin(Time.time * 2.4f) * 0.18f);
            transform.localScale = _baseScale * (1f + Mathf.Sin(Time.time * 4.8f) * 0.045f);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_collected || other.GetComponentInParent<Gameplay.HoverVehicleController>() == null)
            {
                return;
            }

            _collected = true;
            SignalPickupBurst.Spawn(transform.position, GetComponentInChildren<Renderer>()?.sharedMaterial);
            _onCollected?.Invoke(_relicId);
            gameObject.SetActive(false);
        }
    }
}
