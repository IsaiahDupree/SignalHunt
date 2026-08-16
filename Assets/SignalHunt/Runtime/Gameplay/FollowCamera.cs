using UnityEngine;

namespace SignalHunt.Gameplay
{
    public sealed class FollowCamera : MonoBehaviour
    {
        private Transform _target;
        private Vector3 _velocity;
        private bool _cinematic;
        private Camera _camera;
        private HoverVehicleController _vehicle;

        public void SetTarget(Transform target, bool cinematic = false)
        {
            _target = target;
            _cinematic = cinematic;
            _camera = GetComponent<Camera>();
            _vehicle = target == null ? null : target.GetComponent<HoverVehicleController>();
            if (target != null)
            {
                transform.position = DesiredPosition();
            }
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            var desired = DesiredPosition();
            var focusPoint = _target.position + Vector3.up * 0.75f;
            if (!_cinematic && Physics.Linecast(focusPoint, desired, out var hit) &&
                hit.transform.root != _target.root)
            {
                desired = hit.point + (focusPoint - hit.point).normalized * 0.45f;
            }
            var smoothTime = _cinematic ? 0.28f : 0.14f;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime);
            var lookTarget = _target.position + Vector3.up * 0.45f + _target.forward * 2.2f;
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(lookTarget - transform.position, Vector3.up),
                Time.deltaTime * (_cinematic ? 3f : 7f));

            if (_camera != null && !_cinematic)
            {
                var speed = _vehicle == null ? 0f : _vehicle.Speed;
                _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, Mathf.Lerp(62f, 72f, Mathf.Clamp01(speed / 30f)), Time.deltaTime * 3f);
            }
        }

        private Vector3 DesiredPosition()
        {
            if (_cinematic)
            {
                var orbit = Quaternion.Euler(0f, Time.time * 15f, 0f) * new Vector3(7f, 4f, -7f);
                return _target.position + orbit;
            }

            var speed = _vehicle == null ? 0f : _vehicle.Speed;
            var pullback = Mathf.Lerp(6.8f, 8.4f, Mathf.Clamp01(speed / 30f));
            return _target.position - _target.forward * pullback + Vector3.up * 3.45f;
        }
    }
}
