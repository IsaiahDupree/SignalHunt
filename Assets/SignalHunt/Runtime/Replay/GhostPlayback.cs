using SignalHunt.Gameplay;
using UnityEngine;

namespace SignalHunt.Replay
{
    public sealed class GhostPlayback : MonoBehaviour
    {
        private ReplayRun _run;
        private HuntSession _session;
        private int _frameIndex;

        public void Initialize(ReplayRun run, HuntSession session)
        {
            _run = run;
            _session = session;
            gameObject.SetActive(run?.frames != null && run.frames.Count > 1);
        }

        private void Update()
        {
            if (_run?.frames == null || _run.frames.Count < 2 || _session == null)
            {
                return;
            }

            var time = _session.Elapsed;
            while (_frameIndex < _run.frames.Count - 2 && _run.frames[_frameIndex + 1].timestamp <= time)
            {
                _frameIndex++;
            }

            if (time > _run.frames[^1].timestamp)
            {
                gameObject.SetActive(false);
                return;
            }

            var from = _run.frames[_frameIndex];
            var to = _run.frames[Mathf.Min(_frameIndex + 1, _run.frames.Count - 1)];
            var duration = Mathf.Max(0.001f, to.timestamp - from.timestamp);
            var progress = Mathf.Clamp01((time - from.timestamp) / duration);
            transform.position = Vector3.Lerp(from.position, to.position, progress);
            transform.rotation = Quaternion.Slerp(from.rotation, to.rotation, progress);
        }
    }
}
