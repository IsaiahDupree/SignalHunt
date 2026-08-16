using UnityEngine;

namespace SignalHunt.Gameplay
{
    public sealed class FollowCamera : MonoBehaviour
    {
        private Transform _target;
        private Vector3 _velocity;
        private bool _cinematic;

        public void SetTarget(Transform target, bool cinematic = false)
        {
            _target = target;
            _cinematic = cinematic;
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
            var smoothTime = _cinematic ? 0.28f : 0.14f;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime);
            var lookTarget = _target.position + Vector3.up * 0.45f + _target.forward * 2.2f;
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(lookTarget - transform.position, Vector3.up),
                Time.deltaTime * (_cinematic ? 3f : 7f));
        }

        private Vector3 DesiredPosition()
        {
            if (_cinematic)
            {
                var orbit = Quaternion.Euler(0f, Time.time * 15f, 0f) * new Vector3(7f, 4f, -7f);
                return _target.position + orbit;
            }

            return _target.position - _target.forward * 7.8f + Vector3.up * 4.2f;
        }
    }
}
