using System;
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
            BuildRoadGrid(root.transform, palette, layout.worldSize);
            BuildBoundary(root.transform, palette, layout.worldSize);

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

        private static void BuildRoadGrid(Transform parent, GamePalette palette, float worldSize)
        {
            var laneCenters = new[] { -48f, -24f, 0f, 24f, 48f };
            foreach (var lane in laneCenters)
            {
                PrimitiveFactory.Cube("Road Strip H", parent, new Vector3(0f, 0f, lane),
                    new Vector3(worldSize, 0.04f, 7.5f), palette.DarkMetal, false);
                PrimitiveFactory.Cube("Road Strip V", parent, new Vector3(lane, 0.01f, 0f),
                    new Vector3(7.5f, 0.04f, worldSize), palette.DarkMetal, false);

                for (var marker = -5; marker <= 5; marker++)
                {
                    PrimitiveFactory.Cube("Lane Marker", parent, new Vector3(marker * 10f, 0.045f, lane),
                        new Vector3(4.2f, 0.025f, 0.12f), palette.RoadMarking, false);
                    PrimitiveFactory.Cube("Lane Marker", parent, new Vector3(lane, 0.05f, marker * 10f),
                        new Vector3(0.12f, 0.025f, 4.2f), palette.RoadMarking, false);
                }
            }
        }

        private static void BuildBoundary(Transform parent, GamePalette palette, float worldSize)
        {
            var half = worldSize * 0.5f;
            PrimitiveFactory.Cube("North Boundary", parent, new Vector3(0f, 1f, half), new Vector3(worldSize, 2f, 1f), palette.DarkMetal);
            PrimitiveFactory.Cube("South Boundary", parent, new Vector3(0f, 1f, -half), new Vector3(worldSize, 2f, 1f), palette.DarkMetal);
            PrimitiveFactory.Cube("East Boundary", parent, new Vector3(half, 1f, 0f), new Vector3(1f, 2f, worldSize), palette.DarkMetal);
            PrimitiveFactory.Cube("West Boundary", parent, new Vector3(-half, 1f, 0f), new Vector3(1f, 2f, worldSize), palette.DarkMetal);
        }

        private static void BuildBuilding(Transform parent, BuildingDefinition definition, GamePalette palette)
        {
            var building = PrimitiveFactory.Cube(
                definition.isLandmark ? "Landmark" : "Building",
                parent,
                definition.position,
                definition.size,
                palette.BuildingMaterials[definition.materialIndex % palette.BuildingMaterials.Length]);

            var windowMaterial = palette.NeonMaterials[definition.materialIndex % palette.NeonMaterials.Length];
            for (var floor = 2f; floor < definition.size.y - 1f; floor += 3f)
            {
                var localY = -definition.size.y * 0.5f + floor;
                PrimitiveFactory.Cube("Window Band", building.transform,
                    new Vector3(0f, localY / definition.size.y, 0.501f),
                    new Vector3(0.78f, 0.045f, 0.015f), windowMaterial, false);
            }

            PrimitiveFactory.Cube("Roof Light", parent,
                definition.position + Vector3.up * (definition.size.y * 0.5f + 0.12f),
                new Vector3(definition.size.x * 0.65f, 0.16f, definition.size.z * 0.65f), windowMaterial, false);
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
            var trigger = holder.AddComponent<SphereCollider>();
            trigger.radius = 1.45f;
            trigger.isTrigger = true;
            holder.AddComponent<RelicPickup>().Initialize(definition.id, onCollected);
        }
    }
}
