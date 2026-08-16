using UnityEngine;

namespace SignalHunt.Gameplay
{
    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
    public sealed class HoverVehicleController : MonoBehaviour
    {
        private const float HoverHeight = 0.9f;
        private const float MaxForwardSpeed = 30f;
        private const float MaxReverseSpeed = 9f;

        private Rigidbody _body;
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;
        private bool _inputEnabled = true;

        public float Speed => _body == null ? 0f : _body.linearVelocity.magnitude;
        public Rigidbody Body => _body;

        public void Initialize(Vector3 spawnPosition, float heading)
        {
            _body = GetComponent<Rigidbody>();
            _body.mass = 700f;
            _body.linearDamping = 0.45f;
            _body.angularDamping = 3.5f;
            _body.centerOfMass = new Vector3(0f, -0.35f, 0f);
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _spawnPosition = spawnPosition;
            _spawnRotation = Quaternion.Euler(0f, heading, 0f);
            ResetToSpawn();
        }

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            if (!enabled)
            {
                VehicleInputState.Clear();
            }
        }

        public void ResetToSpawn()
        {
            if (_body == null)
            {
                _body = GetComponent<Rigidbody>();
            }

            _body.position = _spawnPosition;
            _body.rotation = _spawnRotation;
            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
        }

        private void FixedUpdate()
        {
            if (_body == null)
            {
                return;
            }

            ApplyHover();
            ApplyGrip();
            if (_inputEnabled)
            {
                ApplyDrive(VehicleInputState.Throttle, VehicleInputState.Steering);
            }

            if (_body.position.y < -4f || transform.up.y < -0.15f)
            {
                ResetToSpawn();
            }
        }

        private void ApplyHover()
        {
            if (Physics.Raycast(_body.worldCenterOfMass, Vector3.down, out var hit, HoverHeight * 2.8f))
            {
                var compression = Mathf.Clamp01((HoverHeight - hit.distance) / HoverHeight);
                var verticalSpeed = Vector3.Dot(_body.linearVelocity, Vector3.up);
                _body.AddForce(Vector3.up * (compression * 52f - verticalSpeed * 7f), ForceMode.Acceleration);
            }

            var upright = Vector3.Cross(transform.up, Vector3.up);
            _body.AddTorque(upright * 9f, ForceMode.Acceleration);
        }

        private void ApplyGrip()
        {
            var localVelocity = transform.InverseTransformDirection(_body.linearVelocity);
            var lateralCorrection = -transform.right * localVelocity.x * 5.5f;
            _body.AddForce(lateralCorrection, ForceMode.Acceleration);
        }

        private void ApplyDrive(float throttle, float steering)
        {
            var forwardSpeed = Vector3.Dot(_body.linearVelocity, transform.forward);
            var canAccelerate = throttle > 0f && forwardSpeed < MaxForwardSpeed;
            var canReverse = throttle < 0f && forwardSpeed > -MaxReverseSpeed;
            if (canAccelerate || canReverse)
            {
                var acceleration = throttle > 0f ? 18f : 10f;
                _body.AddForce(transform.forward * (throttle * acceleration), ForceMode.Acceleration);
            }

            var steeringAuthority = Mathf.Lerp(1.8f, 0.65f, Mathf.Clamp01(Mathf.Abs(forwardSpeed) / MaxForwardSpeed));
            var direction = forwardSpeed < -0.5f ? -1f : 1f;
            _body.AddTorque(Vector3.up * (steering * direction * steeringAuthority * 8f), ForceMode.Acceleration);
        }
    }
}
