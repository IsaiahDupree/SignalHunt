using System.Collections.Generic;
using UnityEngine;

namespace SignalHunt.Visual
{
    public sealed class GamePalette
    {
        private readonly List<Material> _ownedMaterials = new();

        public Material Asphalt { get; }
        public Material Pavement { get; }
        public Material RoadMarking { get; }
        public Material DarkMetal { get; }
        public Material[] BuildingMaterials { get; }
        public Material[] NeonMaterials { get; }
        public Material HoverBody { get; }
        public Material HoverGlass { get; }
        public Material Ghost { get; }

        public GamePalette()
        {
            Asphalt = Create("Asphalt", new Color(0.035f, 0.045f, 0.07f));
            Pavement = Create("Pavement", new Color(0.11f, 0.13f, 0.19f));
            RoadMarking = Create("Road Marking", new Color(0.24f, 0.82f, 1f), true);
            DarkMetal = Create("Dark Metal", new Color(0.025f, 0.03f, 0.055f));
            BuildingMaterials = new[]
            {
                Create("Building Indigo", new Color(0.10f, 0.12f, 0.23f)),
                Create("Building Blue", new Color(0.08f, 0.18f, 0.27f)),
                Create("Building Violet", new Color(0.18f, 0.09f, 0.25f)),
                Create("Building Slate", new Color(0.13f, 0.16f, 0.22f))
            };
            NeonMaterials = new[]
            {
                Create("Signal Cyan", new Color(0.08f, 0.86f, 1f), true),
                Create("Signal Magenta", new Color(1f, 0.12f, 0.67f), true),
                Create("Signal Lime", new Color(0.52f, 1f, 0.2f), true),
                Create("Signal Gold", new Color(1f, 0.7f, 0.12f), true)
            };
            HoverBody = Create("Hover Body", new Color(0.08f, 0.7f, 0.95f), true);
            HoverGlass = Create("Hover Glass", new Color(0.03f, 0.09f, 0.16f));
            Ghost = CreateTransparent("Ghost", new Color(0.12f, 0.95f, 1f, 0.28f));
        }

        public void Dispose()
        {
            foreach (var material in _ownedMaterials)
            {
                if (material != null)
                {
                    Object.Destroy(material);
                }
            }
        }

        public void ApplyVehicleColor(int colorIndex)
        {
            var colors = new[]
            {
                new Color(0.08f, 0.70f, 0.95f),
                new Color(1f, 0.12f, 0.67f),
                new Color(0.52f, 1f, 0.20f),
                new Color(1f, 0.70f, 0.12f)
            };
            var color = colors[Mathf.Abs(colorIndex) % colors.Length];
            HoverBody.color = color;
            if (HoverBody.HasProperty("_EmissionColor"))
            {
                HoverBody.SetColor("_EmissionColor", color * 1.8f);
            }
        }

        private Material Create(string name, Color color, bool emission = false)
        {
            var shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new System.InvalidOperationException("Signal Hunt requires an included lit runtime shader.");
            }
            var material = new Material(shader) { name = name, color = color };
            if (emission && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.8f);
            }

            _ownedMaterials.Add(material);
            return material;
        }

        private Material CreateTransparent(string name, Color color)
        {
            var material = Create(name, color);
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            return material;
        }
    }
}
