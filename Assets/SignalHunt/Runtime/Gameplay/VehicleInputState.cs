using UnityEngine;

namespace SignalHunt.Gameplay
{
    public enum VehicleControl
    {
        Accelerate,
        Brake,
        SteerLeft,
        SteerRight
    }

    public static class VehicleInputState
    {
        private static bool _accelerate;
        private static bool _brake;
        private static bool _left;
        private static bool _right;
        private static float _touchSteering;

        public static float Throttle =>
            Mathf.Clamp((IsKeyboardAccelerating() || _accelerate ? 1f : 0f) -
                        (IsKeyboardBraking() || _brake ? 1f : 0f), -1f, 1f);

        public static float Steering
        {
            get
            {
                var digital = (IsKeyboardRight() || _right ? 1f : 0f) -
                              (IsKeyboardLeft() || _left ? 1f : 0f);
                return Mathf.Abs(digital) > 0.01f ? Mathf.Clamp(digital, -1f, 1f) : _touchSteering;
            }
        }

        public static void SetSteering(float value) => _touchSteering = Mathf.Clamp(value, -1f, 1f);

        public static void Set(VehicleControl control, bool pressed)
        {
            switch (control)
            {
                case VehicleControl.Accelerate: _accelerate = pressed; break;
                case VehicleControl.Brake: _brake = pressed; break;
                case VehicleControl.SteerLeft: _left = pressed; break;
                case VehicleControl.SteerRight: _right = pressed; break;
            }
        }

        public static void Clear()
        {
            _accelerate = _brake = _left = _right = false;
            _touchSteering = 0f;
        }

        private static bool IsKeyboardAccelerating() => Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        private static bool IsKeyboardBraking() => Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        private static bool IsKeyboardLeft() => Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
        private static bool IsKeyboardRight() => Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
    }
}
