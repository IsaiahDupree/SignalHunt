using System;
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
        private Func<float> _speedProvider;
        private float _baseDistance = 6.8f;
        private float _fastDistance = 8.4f;
        private float _height = 3.45f;
        private float _lookAhead = 2.2f;
        private float _minimumFov = 62f;
        private float _maximumFov = 72f;
        private float _speedForMaximumFov = 30f;

        public void ConfigureGameplay(float baseDistance, float fastDistance, float height, float lookAhead,
            float minimumFov, float maximumFov, float speedForMaximumFov)
        {
            _baseDistance = baseDistance;
            _fastDistance = fastDistance;
            _height = height;
            _lookAhead = lookAhead;
            _minimumFov = minimumFov;
            _maximumFov = maximumFov;
            _speedForMaximumFov = Mathf.Max(1f, speedForMaximumFov);
        }

        public void SetSpeedProvider(Func<float> speedProvider)
        {
            _speedProvider = speedProvider;
        }

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
                : _target.position + Vector3.up * 0.45f + _target.forward * _lookAhead;
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(lookTarget - transform.position, Vector3.up),
                Time.deltaTime * (_cinematic ? 3f : 7f));

            if (_camera != null && !_cinematic)
            {
                var speed = CurrentSpeed();
                _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView,
                    Mathf.Lerp(_minimumFov, _maximumFov, Mathf.Clamp01(speed / _speedForMaximumFov)), Time.deltaTime * 3f);
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

            var speed = CurrentSpeed();
            var pullback = Mathf.Lerp(_baseDistance, _fastDistance, Mathf.Clamp01(speed / _speedForMaximumFov));
            return _target.position - _target.forward * pullback + Vector3.up * _height;
        }

        private float CurrentSpeed() => _speedProvider?.Invoke() ?? (_vehicle == null ? 0f : _vehicle.Speed);
    }
}
