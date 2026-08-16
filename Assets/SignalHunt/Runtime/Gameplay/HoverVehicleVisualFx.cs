using System.Collections;
using UnityEngine;

namespace SignalHunt.Gameplay
{
    public sealed class HoverVehicleVisualFx : MonoBehaviour
    {
        private Transform _visualRoot;
        private HoverVehicleController _controller;

        public void Initialize(Transform visualRoot, HoverVehicleController controller)
        {
            _visualRoot = visualRoot;
            _controller = controller;
            StartCoroutine(PrimeTrails());
        }

        private IEnumerator PrimeTrails()
        {
            yield return new WaitForFixedUpdate();
            foreach (var trail in GetComponentsInChildren<TrailRenderer>())
            {
                trail.Clear();
                trail.emitting = true;
            }
        }

        private void LateUpdate()
        {
            if (_visualRoot == null || _controller?.Body == null)
            {
                return;
            }

            var yawRate = _controller.Body.angularVelocity.y;
            var speedLift = Mathf.Clamp01(_controller.Speed / 30f) * 0.06f;
            var pulse = Mathf.Sin(Time.time * 8f) * 0.018f;
            _visualRoot.localPosition = new Vector3(0f, speedLift + pulse, 0f);
            _visualRoot.localRotation = Quaternion.Slerp(_visualRoot.localRotation,
                Quaternion.Euler(0f, 0f, Mathf.Clamp(-yawRate * 4.5f, -8f, 8f)), Time.deltaTime * 6f);
        }
    }
}
