using UnityEngine;

namespace SignalHunt.UI
{
    public static class UiSpriteFactory
    {
        private static Sprite _roundedRectangle;

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
    }
}
