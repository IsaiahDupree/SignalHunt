using UnityEngine;

namespace SignalHunt.Gameplay
{
    public enum ReplayCameraShot
    {
        Orbit,
        Chase,
        Overhead,
        Wide
    }

    public sealed class FollowCamera : MonoBehaviour
    {
        private Transform _target;
        private Vector3 _velocity;
        private bool _cinematic;
        private Camera _camera;
        private HoverVehicleController _vehicle;
        private ReplayCameraShot _cinematicShot;

        public void SetTarget(Transform target, bool cinematic = false, ReplayCameraShot shot = ReplayCameraShot.Orbit)
        {
            _target = target;
            _cinematic = cinematic;
            _cinematicShot = shot;
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
            if (Physics.Linecast(focusPoint, desired, out var hit) &&
                hit.transform.root != _target.root)
            {
                desired = hit.point + (focusPoint - hit.point).normalized * (_cinematic ? 0.9f : 0.45f);
            }
            var smoothTime = _cinematic ? 0.28f : 0.14f;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime);
            var lookTarget = _cinematic && _cinematicShot == ReplayCameraShot.Overhead
                ? _target.position
                : _target.position + Vector3.up * 0.45f + _target.forward * 2.2f;
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
                return _cinematicShot switch
                {
                    ReplayCameraShot.Chase => _target.position - _target.forward * 8.5f + Vector3.up * 3.8f,
                    ReplayCameraShot.Overhead => _target.position - _target.forward * 2f + Vector3.up * 17f,
                    ReplayCameraShot.Wide => _target.position +
                                             Quaternion.Euler(0f, 42f, 0f) * (-_target.forward * 13f) + Vector3.up * 7f,
                    _ => _target.position + Quaternion.Euler(0f, Time.time * 15f, 0f) * new Vector3(7f, 4f, -7f)
                };
            }

            var speed = _vehicle == null ? 0f : _vehicle.Speed;
            var pullback = Mathf.Lerp(6.8f, 8.4f, Mathf.Clamp01(speed / 30f));
            return _target.position - _target.forward * pullback + Vector3.up * 3.45f;
        }
    }
}
