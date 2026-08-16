using System;
using SignalHunt.Visual;
using TreasureHunt.Gameplay;
using UnityEngine;

namespace TreasureHunt.World
{
    public static class TreasureWorldBuilder
    {
        public static GameObject Build(TreasureWorldLayout layout, SignalHunt.Core.DailyChallenge challenge,
            GamePalette palette, Func<string, bool> onArtifactFound)
        {
            var root = new GameObject("Daily Treasure World");
            var ruins = challenge.stageKey == "sunken-ruins";
            PrimitiveFactory.Cube(ruins ? "Ruins Ground" : "Crystal Ground", root.transform,
                new Vector3(0f, -0.65f, 0f), new Vector3(190f, 1.2f, 190f),
                ruins ? palette.GrassMaterials[3] : palette.DarkMetal);
            if (ruins)
            {
                PrimitiveFactory.Cube("Ruins Lagoon", root.transform, new Vector3(-91f, -0.8f, 0f),
                    new Vector3(16f, 0.6f, 190f), palette.Water, false);
            }

            foreach (var path in layout.paths)
            {
                BuildPath(root.transform, path, ruins, palette);
            }
            foreach (var patch in layout.patches)
            {
                BuildGroundPatch(root.transform, patch, ruins, palette);
            }
            foreach (var prop in layout.props)
            {
                BuildProp(root.transform, prop, palette);
            }
            foreach (var artifact in layout.artifacts)
            {
                BuildArtifact(root.transform, artifact, palette, onArtifactFound);
            }
            BuildCenterLandmark(root.transform, ruins, palette);
            return root;
        }

        private static void BuildPath(Transform parent, TreasurePathDefinition path, bool ruins, GamePalette palette)
        {
            var direction = path.end - path.start;
            direction.y = 0f;
            var length = direction.magnitude;
            var midpoint = (path.start + path.end) * 0.5f + Vector3.up * 0.02f;
            var surface = PrimitiveFactory.Cube(ruins ? "Moss Path" : "Crystal Trail", parent, midpoint,
                new Vector3(path.width, 0.15f, length), ruins ? palette.SandMaterials[0] : palette.BuildingMaterials[2],
                false);
            surface.transform.localRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            var markerCount = Mathf.FloorToInt(length / 10f);
            for (var index = 0; index < markerCount; index++)
            {
                var position = Vector3.Lerp(path.start, path.end, (index + 0.5f) / Mathf.Max(1, markerCount)) +
                               Vector3.up * 0.15f;
                var marker = PrimitiveFactory.Cube("Trail Marker", parent, position,
                    new Vector3(0.18f, 0.08f, 1.6f), palette.NeonMaterials[ruins ? 2 : 1], false);
                marker.transform.localRotation = surface.transform.localRotation;
            }
        }

        private static void BuildGroundPatch(Transform parent, TreasureGroundPatch patch, bool ruins,
            GamePalette palette)
        {
            var material = ruins
                ? palette.GrassMaterials[patch.styleIndex % palette.GrassMaterials.Length]
                : palette.BuildingMaterials[patch.styleIndex % palette.BuildingMaterials.Length];
            var instance = PrimitiveFactory.Cylinder("Terrain Patch", parent, patch.position + Vector3.up * 0.02f,
                patch.scale, material, false);
            instance.transform.localScale = new Vector3(patch.scale.x, patch.scale.y, patch.scale.z);
        }

        private static void BuildProp(Transform parent, TreasurePropDefinition prop, GamePalette palette)
        {
            switch (prop.kind)
            {
                case TreasurePropKind.Ruin:
                    BuildRuin(parent, prop, palette);
                    break;
                case TreasurePropKind.Tree:
                    BuildTree(parent, prop, palette);
                    break;
                case TreasurePropKind.Rock:
                    BuildRock(parent, prop, palette);
                    break;
                case TreasurePropKind.Crystal:
                    BuildCrystal(parent, prop, palette);
                    break;
                case TreasurePropKind.Pillar:
                    BuildPillar(parent, prop, palette);
                    break;
            }
        }

        private static void BuildRuin(Transform parent, TreasurePropDefinition prop, GamePalette palette)
        {
            var root = new GameObject("Procedural Ruin");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = prop.position;
            root.transform.localRotation = Quaternion.Euler(0f, prop.heading, 0f);
            var material = palette.RockMaterials[prop.styleIndex % palette.RockMaterials.Length];
            PrimitiveFactory.Cube("Ruin Wall", root.transform, Vector3.up * prop.scale.y * 0.5f,
                prop.scale, material);
            PrimitiveFactory.Cube("Ruin Cap", root.transform, Vector3.up * (prop.scale.y + 0.22f),
                new Vector3(prop.scale.x * 1.08f, 0.34f, prop.scale.z * 1.08f), palette.GrassMaterials[1], false);
            PrimitiveFactory.Cube("Ruin Glyph", root.transform,
                new Vector3(0f, prop.scale.y * 0.58f, prop.scale.z * 0.505f),
                new Vector3(prop.scale.x * 0.42f, prop.scale.y * 0.16f, 0.06f),
                palette.NeonMaterials[prop.styleIndex % 4], false);
        }

        private static void BuildTree(Transform parent, TreasurePropDefinition prop, GamePalette palette)
        {
            var root = new GameObject("Jungle Tree");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = prop.position;
            root.transform.localRotation = Quaternion.Euler(0f, prop.heading, 0f);
            PrimitiveFactory.Cylinder("Tree Trunk", root.transform, Vector3.up * prop.scale.y * 0.45f,
                new Vector3(prop.scale.x * 0.18f, prop.scale.y * 0.45f, prop.scale.z * 0.18f),
                palette.TreeTrunk);
            for (var layer = 0; layer < 3; layer++)
            {
                PrimitiveFactory.Sphere("Jungle Canopy", root.transform,
                    new Vector3((layer - 1) * prop.scale.x * 0.35f, prop.scale.y * (0.78f + layer * 0.08f),
                        (layer % 2 == 0 ? -1f : 1f) * prop.scale.z * 0.18f),
                    new Vector3(prop.scale.x * 0.72f, prop.scale.y * 0.22f, prop.scale.z * 0.72f),
                    palette.PineMaterials[(prop.styleIndex + layer) % palette.PineMaterials.Length], false);
            }
        }

        private static void BuildRock(Transform parent, TreasurePropDefinition prop, GamePalette palette)
        {
            var rock = PrimitiveFactory.Sphere("Cavern Rock", parent,
                prop.position + Vector3.up * prop.scale.y * 0.42f, prop.scale,
                palette.RockMaterials[prop.styleIndex % palette.RockMaterials.Length]);
            rock.transform.localRotation = Quaternion.Euler(prop.styleIndex * 9f, prop.heading, prop.styleIndex * 13f);
        }

        private static void BuildCrystal(Transform parent, TreasurePropDefinition prop, GamePalette palette)
        {
            var root = new GameObject("Luminous Crystal");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = prop.position;
            root.transform.localRotation = Quaternion.Euler(0f, prop.heading, 0f);
            for (var shard = 0; shard < 3; shard++)
            {
                var height = prop.scale.y * (0.62f + shard * 0.18f);
                var instance = PrimitiveFactory.Cube("Crystal Shard", root.transform,
                    new Vector3((shard - 1) * prop.scale.x * 0.45f, height * 0.5f, shard * 0.16f),
                    new Vector3(prop.scale.x * 0.42f, height, prop.scale.z * 0.42f),
                    palette.NeonMaterials[(prop.styleIndex + shard) % 4], false);
                instance.transform.localRotation = Quaternion.Euler(0f, shard * 28f, (shard - 1) * 8f);
            }
        }

        private static void BuildPillar(Transform parent, TreasurePropDefinition prop, GamePalette palette)
        {
            var pillar = PrimitiveFactory.Cylinder("Cavern Pillar", parent,
                prop.position + Vector3.up * prop.scale.y * 0.5f, prop.scale,
                palette.BuildingMaterials[prop.styleIndex % palette.BuildingMaterials.Length]);
            pillar.transform.localRotation = Quaternion.Euler(0f, prop.heading, 0f);
            PrimitiveFactory.Sphere("Pillar Beacon", pillar.transform, new Vector3(0f, 0.55f, 0f),
                new Vector3(0.42f, 0.08f, 0.42f), palette.NeonMaterials[prop.styleIndex % 4], false);
        }

        private static void BuildArtifact(Transform parent, TreasureArtifactDefinition definition,
            GamePalette palette, Func<string, bool> onFound)
        {
            var root = new GameObject($"Treasure Artifact {definition.id}");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = definition.position;
            var material = palette.NeonMaterials[definition.styleIndex % 4];
            var visual = new GameObject("Artifact Visual").transform;
            visual.SetParent(root.transform, false);
            PrimitiveFactory.Cube("Artifact Cache", visual, new Vector3(0f, 0.36f, 0f),
                new Vector3(1.25f, 0.72f, 0.92f), palette.DarkMetal, false);
            PrimitiveFactory.Cube("Cache Lid", visual, new Vector3(0f, 0.80f, 0f),
                new Vector3(1.38f, 0.20f, 1.02f), material, false);
            PrimitiveFactory.Sphere("Artifact Signal", visual, new Vector3(0f, 1.38f, 0f),
                new Vector3(0.48f, 0.48f, 0.48f), material, false);
            PrimitiveFactory.Cube("Artifact Band", visual, new Vector3(0f, 0.44f, 0.47f),
                new Vector3(0.72f, 0.16f, 0.06f), material, false);
            var trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.35f;
            root.AddComponent<TreasureArtifact>().Initialize(definition.id, visual, material, onFound);
        }

        private static void BuildCenterLandmark(Transform parent, bool ruins, GamePalette palette)
        {
            var root = new GameObject(ruins ? "Ruins Compass Tower" : "Crystal Compass Tower");
            root.transform.SetParent(parent, false);
            PrimitiveFactory.Cylinder("Compass Base", root.transform, Vector3.up * 0.7f,
                new Vector3(5f, 0.7f, 5f), ruins ? palette.RockMaterials[0] : palette.BuildingMaterials[2]);
            PrimitiveFactory.Cylinder("Compass Spire", root.transform, Vector3.up * 5f,
                new Vector3(1.1f, 4.3f, 1.1f), palette.DarkMetal);
            PrimitiveFactory.Sphere("Compass Signal", root.transform, Vector3.up * 9.5f,
                new Vector3(1.3f, 1.3f, 1.3f), palette.NeonMaterials[ruins ? 3 : 1], false);
        }
    }

    public sealed class TreasureArtifact : MonoBehaviour
    {
        private string _id;
        private Transform _visual;
        private Material _material;
        private Func<string, bool> _onFound;
        private Vector3 _baseScale;
        private Vector3 _basePosition;
        private float _scanRevealUntil;

        public string ArtifactId => _id;

        public void Initialize(string id, Transform visual, Material material, Func<string, bool> onFound)
        {
            _id = id;
            _visual = visual;
            _material = material;
            _onFound = onFound;
            _baseScale = visual.localScale;
            _basePosition = visual.localPosition;
        }

        public static void ScanAll(Vector3 origin, float radius)
        {
            foreach (var artifact in FindObjectsByType<TreasureArtifact>())
            {
                if (artifact != null && artifact.gameObject.activeInHierarchy &&
                    Vector3.Distance(origin, artifact.transform.position) <= radius)
                {
                    artifact._scanRevealUntil = Time.time + 1.8f;
                }
            }
            ScanPulseVisual.Spawn(origin, radius);
        }

        private void Update()
        {
            if (_visual == null)
            {
                return;
            }
            _visual.Rotate(Vector3.up, 35f * Time.deltaTime, Space.Self);
            var revealed = Time.time < _scanRevealUntil;
            var pulse = 1f + Mathf.Sin(Time.time * (revealed ? 10f : 4f)) * (revealed ? 0.12f : 0.035f);
            _visual.localScale = _baseScale * (revealed ? 1.38f : 1f) * pulse;
            _visual.localPosition = _basePosition + Vector3.up * (Mathf.Sin(Time.time * 3f) * 0.12f);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<ExplorerController>() == null || _onFound == null || !_onFound(_id))
            {
                return;
            }
            SignalHunt.World.SignalPickupBurst.Spawn(transform.position, _material);
            gameObject.SetActive(false);
        }
    }

    internal sealed class ScanPulseVisual : MonoBehaviour
    {
        private float _age;
        private float _radius;

        public static void Spawn(Vector3 origin, float radius)
        {
            var root = new GameObject("Detector Scan Pulse");
            root.transform.position = origin + Vector3.up * 0.12f;
            var line = root.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 48;
            line.widthMultiplier = 0.12f;
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
            if (shader == null)
            {
                Destroy(root);
                return;
            }
            line.sharedMaterial = new Material(shader);
            line.startColor = new Color(0.08f, 0.90f, 1f, 0.9f);
            line.endColor = new Color(1f, 0.70f, 0.12f, 0.2f);
            var pulse = root.AddComponent<ScanPulseVisual>();
            pulse._radius = radius;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            var progress = Mathf.Clamp01(_age / 0.75f);
            var line = GetComponent<LineRenderer>();
            var radius = Mathf.Lerp(0.5f, _radius, progress);
            for (var index = 0; index < line.positionCount; index++)
            {
                var angle = index * Mathf.PI * 2f / line.positionCount;
                line.SetPosition(index, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }
            var color = line.startColor;
            color.a = 1f - progress;
            line.startColor = line.endColor = color;
            if (_age >= 0.75f)
            {
                Destroy(line.sharedMaterial);
                Destroy(gameObject);
            }
        }
    }
}
