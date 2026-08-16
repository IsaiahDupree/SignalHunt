using SignalHunt.Core;
using SignalHunt.Gameplay;
using System.Collections.Generic;
using UnityEngine;

namespace SignalHunt.UI
{
    /// <summary>
    /// Renders the generated daily world as a live, deterministic menu backdrop.
    /// It uses the real gameplay camera and world instead of a bundled video.
    /// </summary>
    public sealed class DailyMenuCameraMotion : MonoBehaviour
    {
        private FollowCamera _followCamera;
        private Transform _focus;
        private Camera _camera;
        private float _radius;
        private float _height;
        private float _lookAhead;
        private float _speed;
        private float _phase;
        private float _menuFov;
        private bool _active;
        private Renderer[] _focusRenderers;
        private bool[] _focusRendererStates;
        private Canvas[] _focusCanvases;
        private bool[] _focusCanvasStates;
        private readonly List<HiddenActor> _hiddenActors = new();

        public void Initialize(FollowCamera followCamera, Transform focus, DailyChallenge challenge)
        {
            _followCamera = followCamera;
            _focus = focus;
            _camera = GetComponent<Camera>();
            _phase = challenge.generationSeed % 360u * Mathf.Deg2Rad;

            switch (challenge.appKey)
            {
                case DailyGameCatalog.TreasureHuntAppKey:
                    _radius = 34f;
                    _height = 14f;
                    _lookAhead = 12f;
                    _speed = 0.060f;
                    _menuFov = 58f;
                    break;
                case DailyGameCatalog.WaypointRallyAppKey:
                    _radius = 42f;
                    _height = 18f;
                    _lookAhead = 15f;
                    _speed = 0.056f;
                    _menuFov = 60f;
                    break;
                case DailyGameCatalog.WaypointWingsAppKey:
                    _radius = 70f;
                    _height = 28f;
                    _lookAhead = 28f;
                    _speed = 0.046f;
                    _menuFov = 62f;
                    break;
                default:
                    _radius = challenge.Stage == WorldStage.Island ? 38f : 32f;
                    _height = challenge.Stage == WorldStage.Island ? 15f : 12f;
                    _lookAhead = 12f;
                    _speed = 0.058f;
                    _menuFov = 59f;
                    break;
            }

            _followCamera.enabled = false;
            CacheFocusVisibility();
            SetFocusVisible(false);
            _active = true;
            PositionCamera(0f, true);
        }

        public void PauseForReplay()
        {
            _active = false;
            if (_followCamera != null)
            {
                _followCamera.enabled = true;
            }
        }

        public void HideActor(GameObject actor)
        {
            if (actor != null)
            {
                _hiddenActors.Add(new HiddenActor(actor));
            }
        }

        public void ResumeAfterReplay()
        {
            if (_focus == null || _followCamera == null)
            {
                return;
            }
            _followCamera.SetTarget(_focus);
            _followCamera.enabled = false;
            _active = true;
            PositionCamera(Time.unscaledTime, true);
        }

        public void Finish()
        {
            _active = false;
            if (_followCamera != null)
            {
                _followCamera.enabled = true;
                _followCamera.SetTarget(_focus);
            }
            RestoreFocusVisibility();
            RestoreHiddenActors();
            Destroy(this);
        }

        private void LateUpdate()
        {
            if (_active)
            {
                PositionCamera(Time.unscaledTime, false);
            }
        }

        private void PositionCamera(float time, bool immediate)
        {
            if (_focus == null)
            {
                return;
            }

            var angle = _phase + time * _speed;
            var forward = _focus.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
            {
                forward = Vector3.forward;
            }
            forward.Normalize();
            var center = _focus.position + forward * _lookAhead;
            var orbit = new Vector3(Mathf.Cos(angle) * _radius, _height + Mathf.Sin(angle * 1.7f) * 2.2f,
                Mathf.Sin(angle) * _radius);
            var desired = center + orbit;
            var lookTarget = center + Vector3.up * (2.5f + _height * 0.18f);

            var blend = immediate ? 1f : 1f - Mathf.Exp(-Time.unscaledDeltaTime * 1.3f);
            transform.position = Vector3.Lerp(transform.position, desired, blend);
            var direction = lookTarget - transform.position;
            if (direction.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(direction, Vector3.up), immediate ? 1f : blend * 1.4f);
            }
            if (_camera != null)
            {
                _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, _menuFov, immediate ? 1f : blend);
            }
        }

        private void OnDestroy()
        {
            RestoreFocusVisibility();
            RestoreHiddenActors();
            if (_followCamera != null)
            {
                _followCamera.enabled = true;
            }
        }

        private void CacheFocusVisibility()
        {
            _focusRenderers = _focus.GetComponentsInChildren<Renderer>(true);
            _focusRendererStates = new bool[_focusRenderers.Length];
            for (var index = 0; index < _focusRenderers.Length; index++)
            {
                _focusRendererStates[index] = _focusRenderers[index].enabled;
            }
            _focusCanvases = _focus.GetComponentsInChildren<Canvas>(true);
            _focusCanvasStates = new bool[_focusCanvases.Length];
            for (var index = 0; index < _focusCanvases.Length; index++)
            {
                _focusCanvasStates[index] = _focusCanvases[index].enabled;
            }
        }

        private void SetFocusVisible(bool visible)
        {
            foreach (var renderer in _focusRenderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = visible;
                }
            }
            foreach (var canvas in _focusCanvases)
            {
                if (canvas != null)
                {
                    canvas.enabled = visible;
                }
            }
        }

        private void RestoreFocusVisibility()
        {
            if (_focusRenderers != null)
            {
                for (var index = 0; index < _focusRenderers.Length; index++)
                {
                    if (_focusRenderers[index] != null)
                    {
                        _focusRenderers[index].enabled = _focusRendererStates[index];
                    }
                }
            }
            if (_focusCanvases != null)
            {
                for (var index = 0; index < _focusCanvases.Length; index++)
                {
                    if (_focusCanvases[index] != null)
                    {
                        _focusCanvases[index].enabled = _focusCanvasStates[index];
                    }
                }
            }
        }

        private void RestoreHiddenActors()
        {
            foreach (var actor in _hiddenActors)
            {
                actor.Restore();
            }
            _hiddenActors.Clear();
        }

        private sealed class HiddenActor
        {
            private readonly Renderer[] _renderers;
            private readonly bool[] _rendererStates;
            private readonly Canvas[] _canvases;
            private readonly bool[] _canvasStates;

            public HiddenActor(GameObject actor)
            {
                _renderers = actor.GetComponentsInChildren<Renderer>(true);
                _rendererStates = new bool[_renderers.Length];
                for (var index = 0; index < _renderers.Length; index++)
                {
                    _rendererStates[index] = _renderers[index].enabled;
                    _renderers[index].enabled = false;
                }
                _canvases = actor.GetComponentsInChildren<Canvas>(true);
                _canvasStates = new bool[_canvases.Length];
                for (var index = 0; index < _canvases.Length; index++)
                {
                    _canvasStates[index] = _canvases[index].enabled;
                    _canvases[index].enabled = false;
                }
            }

            public void Restore()
            {
                for (var index = 0; index < _renderers.Length; index++)
                {
                    if (_renderers[index] != null)
                    {
                        _renderers[index].enabled = _rendererStates[index];
                    }
                }
                for (var index = 0; index < _canvases.Length; index++)
                {
                    if (_canvases[index] != null)
                    {
                        _canvases[index].enabled = _canvasStates[index];
                    }
                }
            }
        }
    }
}
