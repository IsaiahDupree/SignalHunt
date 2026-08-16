using UnityEngine;
using UnityEngine.UI;

namespace SignalHunt.UI
{
    public sealed class WorldNameplate : MonoBehaviour
    {
        private Camera _camera;

        public static WorldNameplate Create(Transform target, string playerName)
        {
            var holder = new GameObject("Player Nameplate");
            holder.transform.SetParent(target, false);
            holder.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            var canvas = holder.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 20;
            var rect = holder.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300f, 54f);
            rect.localScale = Vector3.one * 0.006f;

            var background = holder.AddComponent<Image>();
            background.color = new Color(0.02f, 0.04f, 0.08f, 0.78f);
            var textObject = new GameObject("Name", typeof(RectTransform), typeof(Text));
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(holder.transform, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = playerName;
            text.fontSize = 30;
            text.fontStyle = FontStyle.Bold;
            text.color = new Color(0.08f, 0.86f, 1f);
            text.alignment = TextAnchor.MiddleCenter;

            return holder.AddComponent<WorldNameplate>();
        }

        private void Awake()
        {
            _camera = Camera.main;
        }

        private void LateUpdate()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }
            if (_camera != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - _camera.transform.position, Vector3.up);
            }
        }
    }
}
