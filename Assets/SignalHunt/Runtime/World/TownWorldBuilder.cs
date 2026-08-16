using System;
using SignalHunt.Core;
using SignalHunt.Visual;
using UnityEngine;

namespace SignalHunt.World
{
    public static class TownWorldBuilder
    {
        public static GameObject Build(TownLayout layout, GamePalette palette, Action<string> onRelicCollected)
        {
            var root = new GameObject("Daily Synthetic Town");
            PrimitiveFactory.Cube("City Base", root.transform, new Vector3(0f, -0.55f, 0f),
                new Vector3(layout.worldSize, 1f, layout.worldSize), palette.Asphalt);
            BuildCityBlocks(root.transform, palette);
            BuildRoadGrid(root.transform, palette, layout.worldSize);
            BuildBoundary(root.transform, palette, layout.worldSize);
            BuildSkyline(root.transform, palette, layout.challengeId);

            foreach (var definition in layout.buildings)
            {
                BuildBuilding(root.transform, definition, palette);
            }

            foreach (var definition in layout.props)
            {
                BuildProp(root.transform, definition, palette);
            }

            foreach (var definition in layout.relics)
            {
                BuildRelic(root.transform, definition, palette, onRelicCollected);
            }

            return root;
        }

        private static void BuildCityBlocks(Transform parent, GamePalette palette)
        {
            var centers = new[] { -36f, -12f, 12f, 36f };
            foreach (var x in centers)
            {
                foreach (var z in centers)
                {
                    PrimitiveFactory.Cube("Raised City Block", parent, new Vector3(x, 0.08f, z),
                        new Vector3(18.2f, 0.16f, 18.2f), palette.Sidewalk);
                    PrimitiveFactory.Cube("Block Inlay", parent, new Vector3(x, 0.175f, z),
                        new Vector3(15.8f, 0.035f, 15.8f), palette.Pavement, false);
                }
            }
        }

        private static void BuildRoadGrid(Transform parent, GamePalette palette, float worldSize)
        {
            var laneCenters = new[] { -48f, -24f, 0f, 24f, 48f };
            foreach (var lane in laneCenters)
            {
                PrimitiveFactory.Cube("Road Strip H", parent, new Vector3(0f, 0f, lane),
                    new Vector3(worldSize, 0.04f, 7.5f), palette.DarkMetal, false);
                PrimitiveFactory.Cube("Road Strip V", parent, new Vector3(lane, 0.01f, 0f),
                    new Vector3(7.5f, 0.04f, worldSize), palette.DarkMetal, false);
                PrimitiveFactory.Cube("Road Edge H A", parent, new Vector3(0f, 0.055f, lane - 3.55f),
                    new Vector3(worldSize, 0.025f, 0.07f), palette.RoadEdge, false);
                PrimitiveFactory.Cube("Road Edge H B", parent, new Vector3(0f, 0.055f, lane + 3.55f),
                    new Vector3(worldSize, 0.025f, 0.07f), palette.RoadEdge, false);
                PrimitiveFactory.Cube("Road Edge V A", parent, new Vector3(lane - 3.55f, 0.06f, 0f),
                    new Vector3(0.07f, 0.025f, worldSize), palette.RoadEdge, false);
                PrimitiveFactory.Cube("Road Edge V B", parent, new Vector3(lane + 3.55f, 0.06f, 0f),
                    new Vector3(0.07f, 0.025f, worldSize), palette.RoadEdge, false);

                for (var marker = -5; marker <= 5; marker++)
                {
                    PrimitiveFactory.Cube("Lane Marker", parent, new Vector3(marker * 10f, 0.045f, lane),
                        new Vector3(4.2f, 0.025f, 0.12f), palette.RoadMarking, false);
                    PrimitiveFactory.Cube("Lane Marker", parent, new Vector3(lane, 0.05f, marker * 10f),
                        new Vector3(0.12f, 0.025f, 4.2f), palette.RoadMarking, false);
                }
            }

            var intersections = new[] { -24f, 0f, 24f };
            foreach (var x in intersections)
            {
                foreach (var z in intersections)
                {
                    for (var stripe = -2; stripe <= 2; stripe++)
                    {
                        PrimitiveFactory.Cube("Crosswalk", parent,
                            new Vector3(x + stripe * 0.72f, 0.075f, z - 4.35f),
                            new Vector3(0.38f, 0.025f, 1.25f), palette.RoadMarking, false);
                    }
                }
            }
        }

        private static void BuildBoundary(Transform parent, GamePalette palette, float worldSize)
        {
            var half = worldSize * 0.5f;
            PrimitiveFactory.Cube("North Boundary", parent, new Vector3(0f, 0.45f, half), new Vector3(worldSize, 0.9f, 0.7f), palette.DarkMetal);
            PrimitiveFactory.Cube("South Boundary", parent, new Vector3(0f, 0.45f, -half), new Vector3(worldSize, 0.9f, 0.7f), palette.DarkMetal);
            PrimitiveFactory.Cube("East Boundary", parent, new Vector3(half, 0.45f, 0f), new Vector3(0.7f, 0.9f, worldSize), palette.DarkMetal);
            PrimitiveFactory.Cube("West Boundary", parent, new Vector3(-half, 0.45f, 0f), new Vector3(0.7f, 0.9f, worldSize), palette.DarkMetal);
            PrimitiveFactory.Cube("North Boundary Light", parent, new Vector3(0f, 0.94f, half), new Vector3(worldSize, 0.08f, 0.12f), palette.NeonMaterials[0], false);
            PrimitiveFactory.Cube("South Boundary Light", parent, new Vector3(0f, 0.94f, -half), new Vector3(worldSize, 0.08f, 0.12f), palette.NeonMaterials[1], false);
        }

        private static void BuildBuilding(Transform parent, BuildingDefinition definition, GamePalette palette)
        {
            PrimitiveFactory.Cube("Building Podium", parent,
                new Vector3(definition.position.x, 0.32f, definition.position.z),
                new Vector3(definition.size.x + 1.2f, 0.4f, definition.size.z + 1.2f), palette.DarkMetal);
            PrimitiveFactory.Cube(
                definition.isLandmark ? "Landmark" : "Building",
                parent,
                definition.position,
                definition.size,
                palette.BuildingMaterials[definition.materialIndex % palette.BuildingMaterials.Length]);

            var windowMaterial = palette.NeonMaterials[definition.materialIndex % palette.NeonMaterials.Length];
            var buildingBottom = definition.position.y - definition.size.y * 0.5f;
            for (var floor = 2.2f; floor < definition.size.y - 1f; floor += 2.8f)
            {
                var worldY = buildingBottom + floor;
                PrimitiveFactory.Cube("Window Front", parent,
                    new Vector3(definition.position.x, worldY, definition.position.z + definition.size.z * 0.501f),
                    new Vector3(definition.size.x * 0.68f, 0.24f, 0.045f), windowMaterial, false);
                PrimitiveFactory.Cube("Window Back", parent,
                    new Vector3(definition.position.x, worldY, definition.position.z - definition.size.z * 0.501f),
                    new Vector3(definition.size.x * 0.68f, 0.24f, 0.045f), windowMaterial, false);
                PrimitiveFactory.Cube("Window Left", parent,
                    new Vector3(definition.position.x - definition.size.x * 0.501f, worldY, definition.position.z),
                    new Vector3(0.045f, 0.24f, definition.size.z * 0.68f), windowMaterial, false);
                PrimitiveFactory.Cube("Window Right", parent,
                    new Vector3(definition.position.x + definition.size.x * 0.501f, worldY, definition.position.z),
                    new Vector3(0.045f, 0.24f, definition.size.z * 0.68f), windowMaterial, false);
            }

            PrimitiveFactory.Cube("Facade Spine", parent,
                new Vector3(definition.position.x - definition.size.x * 0.34f, definition.position.y,
                    definition.position.z + definition.size.z * 0.505f),
                new Vector3(0.12f, definition.size.y * 0.82f, 0.055f), windowMaterial, false);

            PrimitiveFactory.Cube("Roof Light", parent,
                definition.position + Vector3.up * (definition.size.y * 0.5f + 0.12f),
                new Vector3(definition.size.x * 0.65f, 0.16f, definition.size.z * 0.65f), windowMaterial, false);
            if (definition.isLandmark)
            {
                PrimitiveFactory.Cube("Rooftop Antenna", parent,
                    definition.position + Vector3.up * (definition.size.y * 0.5f + 1.2f),
                    new Vector3(0.12f, 2.3f, 0.12f), windowMaterial, false);
            }
        }

        private static void BuildSkyline(Transform parent, GamePalette palette, string challengeId)
        {
            var random = new DeterministicRandom(DailyChallenge.StableHash(challengeId + "|visuals-v1"));
            for (var index = 0; index < 22; index++)
            {
                var angle = random.Range(0f, Mathf.PI * 2f);
                var distance = random.Range(74f, 105f);
                var height = random.Range(12f, 42f);
                var position = new Vector3(Mathf.Cos(angle) * distance, height * 0.5f - 1f, Mathf.Sin(angle) * distance);
                PrimitiveFactory.Cube("Distant Skyline", parent, position,
                    new Vector3(random.Range(5f, 12f), height, random.Range(5f, 12f)),
                    palette.BuildingMaterials[random.Range(0, palette.BuildingMaterials.Length)], false);
                PrimitiveFactory.Cube("Skyline Crown", parent, position + Vector3.up * (height * 0.5f + 0.15f),
                    new Vector3(random.Range(2f, 5f), 0.15f, random.Range(2f, 5f)),
                    palette.NeonMaterials[random.Range(0, palette.NeonMaterials.Length)], false);
            }

            PrimitiveFactory.Sphere("Synthetic Moon", parent, new Vector3(-76f, 58f, 86f),
                Vector3.one * 14f, palette.NeonMaterials[0], false);
        }

        private static void BuildProp(Transform parent, PropDefinition definition, GamePalette palette)
        {
            var material = palette.NeonMaterials[definition.materialIndex % palette.NeonMaterials.Length];
            switch (definition.kind)
            {
                case TownPropKind.Ramp:
                {
                    var ramp = PrimitiveFactory.Cube("Hover Ramp", parent, definition.position, definition.scale, palette.Pavement);
                    ramp.transform.rotation = Quaternion.Euler(-8f, definition.yaw, 0f);
                    PrimitiveFactory.Cube("Ramp Signal", ramp.transform, new Vector3(0f, 0.54f, 0f),
                        new Vector3(0.8f, 0.06f, 0.82f), material, false);
                    break;
                }
                case TownPropKind.SignalTower:
                    PrimitiveFactory.Cube("Signal Tower", parent,
                        definition.position + Vector3.up * definition.scale.y * 0.5f,
                        definition.scale, material);
                    break;
                case TownPropKind.LightPylon:
                    PrimitiveFactory.Cube("Light Pylon", parent,
                        definition.position + Vector3.up * definition.scale.y * 0.5f,
                        definition.scale, material, false);
                    break;
                case TownPropKind.TunnelGate:
                    BuildTunnelGate(parent, definition, palette, material);
                    break;
            }
        }

        private static void BuildTunnelGate(Transform parent, PropDefinition definition, GamePalette palette, Material material)
        {
            var gate = new GameObject("Tunnel Gate");
            gate.transform.SetParent(parent, false);
            gate.transform.position = definition.position;
            gate.transform.rotation = Quaternion.Euler(0f, definition.yaw, 0f);
            PrimitiveFactory.Cube("Gate Left", gate.transform, new Vector3(-3.5f, 1.7f, 0f),
                new Vector3(0.5f, 3.4f, 0.7f), palette.Pavement);
            PrimitiveFactory.Cube("Gate Right", gate.transform, new Vector3(3.5f, 1.7f, 0f),
                new Vector3(0.5f, 3.4f, 0.7f), palette.Pavement);
            PrimitiveFactory.Cube("Gate Top", gate.transform, new Vector3(0f, 3.4f, 0f),
                new Vector3(7.5f, 0.5f, 0.7f), material);
        }

        private static void BuildRelic(Transform parent, RelicDefinition definition, GamePalette palette, Action<string> onCollected)
        {
            var holder = new GameObject(definition.id);
            holder.transform.SetParent(parent, false);
            holder.transform.position = definition.position;

            var visual = PrimitiveFactory.Sphere("Signal Orb", holder.transform, Vector3.zero,
                definition.bonus ? Vector3.one * 1.05f : Vector3.one * 0.82f,
                palette.NeonMaterials[definition.styleIndex % palette.NeonMaterials.Length], false);
            visual.transform.localScale = new Vector3(0.72f, 1f, 0.72f);
            var relicMaterial = palette.NeonMaterials[definition.styleIndex % palette.NeonMaterials.Length];
            for (var segment = 0; segment < 12; segment++)
            {
                var angle = segment * Mathf.PI * 2f / 12f;
                var segmentObject = PrimitiveFactory.Cube("Orbital Ring", holder.transform,
                    new Vector3(Mathf.Cos(angle) * 1.12f, 0f, Mathf.Sin(angle) * 1.12f),
                    new Vector3(0.34f, 0.055f, 0.11f), relicMaterial, false);
                segmentObject.transform.localRotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);
            }
            PrimitiveFactory.Cube("Signal Beam", holder.transform, new Vector3(0f, 2.3f, 0f),
                new Vector3(0.055f, 3.6f, 0.055f), relicMaterial, false);
            var trigger = holder.AddComponent<SphereCollider>();
            trigger.radius = 1.45f;
            trigger.isTrigger = true;
            holder.AddComponent<RelicPickup>().Initialize(definition.id, onCollected);
        }
    }
}
