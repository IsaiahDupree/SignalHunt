using System;
using SignalHunt.Visual;
using UnityEngine;

namespace WaypointRally.World
{
    public static class RallyWorldBuilder
    {
        public static GameObject Build(RallyCourseLayout layout, SignalHunt.Core.DailyChallenge challenge,
            GamePalette palette, Func<int, string, bool> onCheckpointPassed)
        {
            var root = new GameObject("Daily Rally Course");
            var island = challenge.stageKey == "island-loop";
            var town = challenge.stageKey == "harbor-town";
            if (island)
            {
                PrimitiveFactory.Cube("Island Water", root.transform, new Vector3(0f, -1.25f, 0f),
                    new Vector3(320f, 0.7f, 320f), palette.Water, false);
                PrimitiveFactory.Cylinder("Island Sand", root.transform, new Vector3(0f, -0.72f, 0f),
                    new Vector3(220f, 0.7f, 220f), palette.SandMaterials[0], false);
                PrimitiveFactory.Cylinder("Island Grass", root.transform, new Vector3(0f, -0.42f, 0f),
                    new Vector3(204f, 0.62f, 204f), palette.GrassMaterials[1], false);
            }
            else
            {
                PrimitiveFactory.Cube(town ? "Harbor Ground" : "Dustlands Ground", root.transform,
                    new Vector3(0f, -0.65f, 0f), new Vector3(220f, 1.2f, 220f),
                    town ? palette.GrassMaterials[1] : palette.SandMaterials[0]);
                if (town)
                {
                    PrimitiveFactory.Cube("Harbor Water", root.transform, new Vector3(-126f, -0.9f, 0f),
                        new Vector3(34f, 0.7f, 220f), palette.Water, false);
                }
            }

            foreach (var road in layout.roads)
            {
                BuildRoad(root.transform, road, island || town, palette);
            }
            foreach (var building in layout.buildings)
            {
                BuildBuilding(root.transform, building, palette);
            }
            foreach (var obstacle in layout.obstacles)
            {
                BuildRock(root.transform, obstacle, palette);
            }
            foreach (var decoration in layout.decorations)
            {
                if (island || town)
                {
                    BuildHarborTree(root.transform, decoration, palette);
                }
                else
                {
                    BuildCactus(root.transform, decoration, palette);
                }
            }
            foreach (var ramp in layout.ramps)
            {
                BuildRamp(root.transform, ramp, palette);
            }
            for (var index = 0; index < layout.checkpoints.Count; index++)
            {
                BuildCheckpoint(root.transform, layout.checkpoints[index], index, palette, onCheckpointPassed);
            }
            return root;
        }

        private static void BuildRoad(Transform parent, RallyRoadDefinition road, bool paved, GamePalette palette)
        {
            var direction = road.end - road.start;
            direction.y = 0f;
            var length = direction.magnitude;
            var midpoint = (road.start + road.end) * 0.5f + Vector3.up * 0.02f;
            var heading = Quaternion.LookRotation(direction.normalized, Vector3.up);
            var shoulder = PrimitiveFactory.Cube("Road Shoulder", parent, midpoint,
                new Vector3(road.width + 2.4f, 0.18f, length + 1.2f), palette.RoadShoulder, false);
            shoulder.transform.localRotation = heading;
            var surface = PrimitiveFactory.Cube(paved ? "Island Road" : "Dust Track", parent,
                midpoint + Vector3.up * 0.10f, new Vector3(road.width, 0.16f, length),
                paved ? palette.IslandRoad : palette.SandMaterials[1], false);
            surface.transform.localRotation = heading;
            if (!paved)
            {
                return;
            }
            var dashCount = Mathf.FloorToInt(length / 8f);
            for (var index = 0; index < dashCount; index++)
            {
                var progress = (index + 0.5f) / Mathf.Max(1, dashCount);
                var position = Vector3.Lerp(road.start, road.end, progress) + Vector3.up * 0.24f;
                var dash = PrimitiveFactory.Cube("Road Dash", parent, position,
                    new Vector3(0.18f, 0.05f, 2.7f), palette.RoadMarking, false);
                dash.transform.localRotation = heading;
            }
        }

        private static void BuildBuilding(Transform parent, RallyPropDefinition definition, GamePalette palette)
        {
            var position = definition.position + Vector3.up * definition.scale.y * 0.5f;
            var building = PrimitiveFactory.Cube("Harbor Building", parent, position, definition.scale,
                palette.BuildingMaterials[definition.styleIndex % palette.BuildingMaterials.Length]);
            building.transform.localRotation = Quaternion.Euler(0f, definition.heading, 0f);
            for (var floor = 1; floor < Mathf.Max(2, Mathf.FloorToInt(definition.scale.y / 4f)); floor++)
            {
                PrimitiveFactory.Cube("Window Band", building.transform,
                    new Vector3(0f, -0.5f + floor / Mathf.Max(2f, definition.scale.y / 4f), 0.505f),
                    new Vector3(0.78f, 0.045f, 0.018f), palette.NeonMaterials[(definition.styleIndex + floor) % 4], false);
            }
            PrimitiveFactory.Cube("Roof Beacon", building.transform, new Vector3(0f, 0.54f, 0f),
                new Vector3(0.82f, 0.04f, 0.82f), palette.NeonMaterials[definition.styleIndex % 4], false);
        }

        private static void BuildRock(Transform parent, RallyPropDefinition definition, GamePalette palette)
        {
            var rock = PrimitiveFactory.Sphere("Dust Rock", parent,
                definition.position + Vector3.up * definition.scale.y * 0.45f, definition.scale,
                palette.RockMaterials[definition.styleIndex % palette.RockMaterials.Length]);
            rock.transform.localRotation = Quaternion.Euler(definition.styleIndex * 11f, definition.heading,
                definition.styleIndex * 7f);
        }

        private static void BuildHarborTree(Transform parent, RallyPropDefinition definition, GamePalette palette)
        {
            var tree = new GameObject("Harbor Palm");
            tree.transform.SetParent(parent, false);
            tree.transform.localPosition = definition.position;
            tree.transform.localRotation = Quaternion.Euler(0f, definition.heading, 0f);
            PrimitiveFactory.Cylinder("Palm Trunk", tree.transform, Vector3.up * definition.scale.y * 0.5f,
                new Vector3(definition.scale.x * 0.24f, definition.scale.y * 0.5f, definition.scale.z * 0.24f),
                palette.TreeTrunk, false);
            for (var frond = 0; frond < 5; frond++)
            {
                var leaf = PrimitiveFactory.Cube("Palm Frond", tree.transform,
                    Vector3.up * definition.scale.y + Quaternion.Euler(0f, frond * 72f, 0f) * Vector3.forward * 0.7f,
                    new Vector3(0.35f, 0.12f, 2.4f), palette.PineMaterials[frond % palette.PineMaterials.Length], false);
                leaf.transform.localRotation = Quaternion.Euler(12f, frond * 72f, 0f);
            }
        }

        private static void BuildCactus(Transform parent, RallyPropDefinition definition, GamePalette palette)
        {
            var cactus = new GameObject("Dust Cactus");
            cactus.transform.SetParent(parent, false);
            cactus.transform.localPosition = definition.position;
            cactus.transform.localRotation = Quaternion.Euler(0f, definition.heading, 0f);
            var material = palette.GrassMaterials[(definition.styleIndex + 1) % palette.GrassMaterials.Length];
            PrimitiveFactory.Cylinder("Cactus Stem", cactus.transform, Vector3.up * definition.scale.y * 0.5f,
                new Vector3(definition.scale.x, definition.scale.y * 0.5f, definition.scale.z), material, false);
            PrimitiveFactory.Cylinder("Cactus Arm", cactus.transform,
                new Vector3(definition.scale.x * 1.2f, definition.scale.y * 0.55f, 0f),
                new Vector3(definition.scale.x * 0.55f, definition.scale.y * 0.24f, definition.scale.z * 0.55f),
                material, false);
        }

        private static void BuildRamp(Transform parent, RallyRampDefinition definition, GamePalette palette)
        {
            var ramp = PrimitiveFactory.Cube("Rally Ramp", parent, definition.position, definition.scale,
                palette.DarkMetal);
            ramp.transform.localRotation = Quaternion.Euler(-10f, definition.heading, 0f);
            PrimitiveFactory.Cube("Ramp Stripe", ramp.transform, new Vector3(0f, 0.51f, 0.18f),
                new Vector3(0.76f, 0.03f, 0.16f), palette.NeonMaterials[3], false);
        }

        private static void BuildCheckpoint(Transform parent, RallyCheckpointDefinition definition, int index,
            GamePalette palette, Func<int, string, bool> onPassed)
        {
            var checkpoint = new GameObject($"Rally Checkpoint {index + 1:D2}");
            checkpoint.transform.SetParent(parent, false);
            checkpoint.transform.localPosition = definition.position;
            checkpoint.transform.localRotation = Quaternion.Euler(0f, definition.heading, 0f);
            var material = palette.NeonMaterials[definition.styleIndex % 4];
            PrimitiveFactory.Cube("Left Pylon", checkpoint.transform, new Vector3(-definition.width * 0.5f, 1.65f, 0f),
                new Vector3(0.55f, 3.3f, 0.55f), material, false);
            PrimitiveFactory.Cube("Right Pylon", checkpoint.transform, new Vector3(definition.width * 0.5f, 1.65f, 0f),
                new Vector3(0.55f, 3.3f, 0.55f), material, false);
            PrimitiveFactory.Cube("Checkpoint Beam", checkpoint.transform, new Vector3(0f, 3.2f, 0f),
                new Vector3(definition.width + 0.55f, 0.42f, 0.55f), material, false);
            PrimitiveFactory.Cube("Checkpoint Arrow", checkpoint.transform, new Vector3(0f, 3.72f, 0f),
                new Vector3(2.1f, 0.18f, 0.72f), palette.DarkMetal, false);
            var trigger = checkpoint.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1.35f, 0f);
            trigger.size = new Vector3(definition.width, 2.7f, 3.5f);
            checkpoint.AddComponent<RallyCheckpoint>().Initialize(index, definition.id, onPassed);
        }
    }

    public sealed class RallyCheckpoint : MonoBehaviour
    {
        private int _index;
        private string _id;
        private Func<int, string, bool> _onPassed;
        private Vector3 _baseScale;

        public int Index => _index;
        public string CheckpointId => _id;

        public void Initialize(int index, string id, Func<int, string, bool> onPassed)
        {
            _index = index;
            _id = id;
            _onPassed = onPassed;
            _baseScale = transform.localScale;
        }

        private void Update()
        {
            transform.localScale = _baseScale * (1f + Mathf.Sin(Time.time * 3f + _index) * 0.018f);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<SignalHunt.Gameplay.HoverVehicleController>() == null ||
                _onPassed == null || !_onPassed(_index, _id))
            {
                return;
            }
            SignalHunt.World.SignalPickupBurst.Spawn(transform.position,
                GetComponentInChildren<Renderer>()?.sharedMaterial);
            gameObject.SetActive(false);
        }
    }
}
