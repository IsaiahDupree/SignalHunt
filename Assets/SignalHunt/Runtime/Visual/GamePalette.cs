using System.Collections.Generic;
using UnityEngine;

namespace SignalHunt.Visual
{
    public sealed class GamePalette
    {
        private readonly List<Material> _ownedMaterials = new();

        public Material Asphalt { get; }
        public Material Pavement { get; }
        public Material Sidewalk { get; }
        public Material RoadMarking { get; }
        public Material RoadEdge { get; }
        public Material DarkMetal { get; }
        public Material WindowGlass { get; }
        public Material Water { get; }
        public Material IslandRoad { get; }
        public Material RoadShoulder { get; }
        public Material TreeTrunk { get; }
        public Material[] BuildingMaterials { get; }
        public Material[] NeonMaterials { get; }
        public Material[] GrassMaterials { get; }
        public Material[] SandMaterials { get; }
        public Material[] PineMaterials { get; }
        public Material[] RockMaterials { get; }
        public Material HoverBody { get; }
        public Material HoverGlass { get; }
        public Material VehicleTrail { get; }
        public Material Ghost { get; }

        public GamePalette()
        {
            Asphalt = Create("Asphalt", new Color(0.026f, 0.035f, 0.058f), false, 0.18f, 0.15f);
            Pavement = Create("Pavement", new Color(0.12f, 0.15f, 0.22f), false, 0.35f, 0.22f);
            Sidewalk = Create("Sidewalk", new Color(0.17f, 0.20f, 0.29f), false, 0.42f, 0.16f);
            RoadMarking = Create("Road Marking", new Color(0.10f, 0.72f, 1f), true);
            RoadEdge = Create("Road Edge", new Color(0.45f, 0.16f, 0.82f), true);
            DarkMetal = Create("Dark Metal", new Color(0.018f, 0.025f, 0.05f), false, 0.5f, 0.5f);
            WindowGlass = Create("Window Glass", new Color(0.025f, 0.08f, 0.14f), false, 0.86f, 0.35f);
            Water = Create("Island Water", new Color(0.08f, 0.42f, 0.58f), false, 0.92f, 0.16f);
            IslandRoad = Create("Island Asphalt", new Color(0.055f, 0.09f, 0.12f), false, 0.32f, 0.18f);
            RoadShoulder = Create("Island Road Shoulder", new Color(0.48f, 0.34f, 0.19f), false, 0.18f, 0.04f);
            TreeTrunk = Create("Pine Trunk", new Color(0.20f, 0.12f, 0.07f), false, 0.22f, 0.02f);
            GrassMaterials = new[]
            {
                Create("Grass Lime", new Color(0.18f, 0.72f, 0.17f), false, 0.14f, 0f),
                Create("Grass Green", new Color(0.10f, 0.58f, 0.12f), false, 0.14f, 0f),
                Create("Grass Bright", new Color(0.28f, 0.82f, 0.19f), false, 0.14f, 0f),
                Create("Grass Deep", new Color(0.07f, 0.43f, 0.11f), false, 0.14f, 0f)
            };
            SandMaterials = new[]
            {
                Create("Warm Sand", new Color(0.78f, 0.65f, 0.39f), false, 0.16f, 0f),
                Create("Light Sand", new Color(0.91f, 0.83f, 0.58f), false, 0.16f, 0f)
            };
            PineMaterials = new[]
            {
                Create("Pine Forest", new Color(0.025f, 0.24f, 0.13f), false, 0.2f, 0f),
                Create("Pine Emerald", new Color(0.035f, 0.34f, 0.16f), false, 0.2f, 0f),
                Create("Pine Dark", new Color(0.015f, 0.16f, 0.11f), false, 0.2f, 0f)
            };
            RockMaterials = new[]
            {
                Create("Island Rock Slate", new Color(0.29f, 0.34f, 0.38f), false, 0.18f, 0.02f),
                Create("Island Rock Moss", new Color(0.27f, 0.38f, 0.25f), false, 0.18f, 0.02f),
                Create("Island Rock Warm", new Color(0.42f, 0.38f, 0.31f), false, 0.18f, 0.02f)
            };
            BuildingMaterials = new[]
            {
                Create("Building Indigo", new Color(0.13f, 0.16f, 0.31f), false, 0.48f, 0.24f),
                Create("Building Blue", new Color(0.08f, 0.22f, 0.34f), false, 0.48f, 0.24f),
                Create("Building Violet", new Color(0.23f, 0.10f, 0.32f), false, 0.48f, 0.24f),
                Create("Building Slate", new Color(0.17f, 0.20f, 0.29f), false, 0.48f, 0.24f)
            };
            NeonMaterials = new[]
            {
                Create("Signal Cyan", new Color(0.08f, 0.86f, 1f), true),
                Create("Signal Magenta", new Color(1f, 0.12f, 0.67f), true),
                Create("Signal Lime", new Color(0.52f, 1f, 0.2f), true),
                Create("Signal Gold", new Color(1f, 0.7f, 0.12f), true)
            };
            HoverBody = Create("Hover Body", new Color(0.08f, 0.7f, 0.95f), true, 0.72f, 0.38f);
            HoverGlass = Create("Hover Glass", new Color(0.015f, 0.07f, 0.12f), false, 0.94f, 0.28f);
            VehicleTrail = CreateTransparent("Vehicle Trail", new Color(1f, 0.12f, 0.67f, 0.58f));
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

        private Material Create(string name, Color color, bool emission = false, float smoothness = 0.55f, float metallic = 0.08f)
        {
            var shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new System.InvalidOperationException("Signal Hunt requires an included lit runtime shader.");
            }
            var material = new Material(shader) { name = name, color = color };
            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", smoothness);
            }
            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }
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
