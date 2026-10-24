using UnityEngine;

namespace SignalHunt.UI
{
    public static class UiSpriteFactory
    {
        private static Sprite _roundedRectangle;
        private static Sprite _circle;
        private static Sprite _ring;

        public static Sprite RoundedRectangle
        {
            get
            {
                if (_roundedRectangle == null)
                {
                    _roundedRectangle = CreateRoundedRectangle();
                }
                return _roundedRectangle;
            }
        }

        public static Sprite Circle => _circle == null ? _circle = CreateRadialSprite(false) : _circle;
        public static Sprite Ring => _ring == null ? _ring = CreateRadialSprite(true) : _ring;

        private static Sprite CreateRoundedRectangle()
        {
            const int size = 64;
            const float radius = 16f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Signal Hunt Rounded UI",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var point = new Vector2(x + 0.5f, y + 0.5f);
                    var clamped = new Vector2(
                        Mathf.Clamp(point.x, radius, size - radius),
                        Mathf.Clamp(point.y, radius, size - radius));
                    var distance = Vector2.Distance(point, clamped);
                    var alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(radius + 0.5f - distance) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 64f,
                0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        private static Sprite CreateRadialSprite(bool ring)
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = ring ? "Signal Hunt Ring UI" : "Signal Hunt Circle UI",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color32[size * size];
            var center = new Vector2(size * 0.5f, size * 0.5f);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    var alpha = ring
                        ? Mathf.Clamp01(1.75f - Mathf.Abs(distance - 27.5f))
                        : Mathf.Clamp01(31.5f - distance);
                    pixels[y * size + x] = new Color32(255, 255, 255,
                        (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 64f);
        }
    }
}
