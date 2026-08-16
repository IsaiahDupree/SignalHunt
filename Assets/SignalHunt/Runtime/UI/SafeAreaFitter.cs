using UnityEngine;

namespace SignalHunt.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private Rect _lastSafeArea;
        private Vector2Int _lastResolution;

        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            if (_lastSafeArea != Screen.safeArea || _lastResolution.x != Screen.width || _lastResolution.y != Screen.height)
            {
                Apply();
            }
        }

        private void Apply()
        {
            var safeArea = Screen.safeArea;
            var rect = GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(safeArea.xMin / Screen.width, safeArea.yMin / Screen.height);
            rect.anchorMax = new Vector2(safeArea.xMax / Screen.width, safeArea.yMax / Screen.height);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            _lastSafeArea = safeArea;
            _lastResolution = new Vector2Int(Screen.width, Screen.height);
        }
    }
}
