using System;
using SignalHunt.Visual;
using UnityEngine;

namespace WaypointWings.World
{
    public static class FlightWorldBuilder
    {
        public static GameObject Build(FlightCourseLayout layout, GamePalette palette, Func<int, string, bool> onGatePassed)
        {
            var root = new GameObject("Daily Flight Course");
            PrimitiveFactory.Cube("Ocean Horizon", root.transform, new Vector3(0f, -4.5f, 70f),
                new Vector3(420f, 1f, 520f), palette.Water, false);

            foreach (var island in layout.islands)
            {
                BuildIsland(root.transform, island, palette);
            }
            foreach (var tower in layout.towers)
            {
                BuildTower(root.transform, tower, palette);
            }
            foreach (var cloud in layout.clouds)
            {
                BuildCloud(root.transform, cloud, palette);
            }
            for (var index = 0; index < layout.gates.Count; index++)
            {
                BuildGate(root.transform, layout.gates[index], index, palette, onGatePassed);
            }
            return root;
        }

        private static void BuildIsland(Transform parent, SkyIslandDefinition definition, GamePalette palette)
        {
            var island = new GameObject("Sky Island");
            island.transform.SetParent(parent, false);
            island.transform.localPosition = definition.position;
            var top = PrimitiveFactory.Cylinder("Island Grass", island.transform, Vector3.zero,
                definition.scale, palette.GrassMaterials[definition.styleIndex % palette.GrassMaterials.Length]);
            top.transform.localRotation = Quaternion.Euler(0f, definition.styleIndex * 19f, 0f);
            var rock = PrimitiveFactory.Cylinder("Island Rock Base", island.transform,
                new Vector3(0f, -definition.scale.y * 1.15f, 0f),
                new Vector3(definition.scale.x * 0.78f, definition.scale.y * 0.72f, definition.scale.z * 0.78f),
                palette.RockMaterials[definition.styleIndex % palette.RockMaterials.Length], false);
            rock.transform.localRotation = Quaternion.Euler(180f, definition.styleIndex * 23f, 0f);
            var peakCount = 1 + definition.styleIndex % 3;
            for (var index = 0; index < peakCount; index++)
            {
                var offset = new Vector3((index - (peakCount - 1) * 0.5f) * 3.2f, definition.scale.y * 1.18f,
                    (index % 2 == 0 ? -1f : 1f) * 1.8f);
                PrimitiveFactory.Cylinder("Island Pine", island.transform, offset,
                    new Vector3(0.65f, 2.4f + index * 0.4f, 0.65f),
                    palette.PineMaterials[(definition.styleIndex + index) % palette.PineMaterials.Length], false);
            }
        }

        private static void BuildTower(Transform parent, SkyTowerDefinition definition, GamePalette palette)
        {
            var tower = PrimitiveFactory.Cube("Skyway Tower", parent, definition.position, definition.scale,
                palette.BuildingMaterials[definition.styleIndex % palette.BuildingMaterials.Length]);
            for (var band = -2; band <= 2; band++)
            {
                PrimitiveFactory.Cube("Tower Light Band", tower.transform,
                    new Vector3(0f, band * 0.18f, 0.505f), new Vector3(0.82f, 0.018f, 0.02f),
                    palette.NeonMaterials[(definition.styleIndex + band + 8) % 4], false);
            }
        }

        private static void BuildCloud(Transform parent, CloudDefinition definition, GamePalette palette)
        {
            var cloud = new GameObject("Procedural Cloud");
            cloud.transform.SetParent(parent, false);
            cloud.transform.localPosition = definition.position;
            var material = palette.SandMaterials[1];
            PrimitiveFactory.Sphere("Cloud Core", cloud.transform, Vector3.zero, definition.scale, material, false);
            PrimitiveFactory.Sphere("Cloud Left", cloud.transform, new Vector3(-definition.scale.x * 0.52f, 0f, 0f),
                definition.scale * 0.72f, material, false);
            PrimitiveFactory.Sphere("Cloud Right", cloud.transform, new Vector3(definition.scale.x * 0.50f, 0.2f, 0f),
                definition.scale * 0.68f, material, false);
        }

        private static void BuildGate(Transform parent, FlightGateDefinition definition, int index,
            GamePalette palette, Func<int, string, bool> onGatePassed)
        {
            var gate = new GameObject($"Flight Gate {index + 1:D2}");
            gate.transform.SetParent(parent, false);
            gate.transform.localPosition = definition.position;
            gate.transform.localRotation = definition.rotation;
            const int segments = 24;
            for (var segment = 0; segment < segments; segment++)
            {
                var angle = segment * Mathf.PI * 2f / segments;
                var position = new Vector3(Mathf.Cos(angle) * definition.radius, Mathf.Sin(angle) * definition.radius, 0f);
                var piece = PrimitiveFactory.Cube("Gate Segment", gate.transform, position,
                    new Vector3(0.62f, definition.radius * 0.27f, 0.56f),
                    palette.NeonMaterials[definition.styleIndex % 4], false);
                piece.transform.localRotation = Quaternion.Euler(0f, 0f, -angle * Mathf.Rad2Deg);
            }

            PrimitiveFactory.Cube("Gate Number Bar", gate.transform, new Vector3(0f, definition.radius + 1.4f, 0f),
                new Vector3(3.2f, 0.25f, 0.35f), palette.DarkMetal, false);
            var trigger = gate.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = definition.radius * 0.78f;
            gate.AddComponent<FlightGate>().Initialize(index, definition.id, onGatePassed);
        }
    }

    public sealed class FlightGate : MonoBehaviour
    {
        private int _index;
        private string _id;
        private Func<int, string, bool> _onPassed;
        private Vector3 _baseScale;

        public int Index => _index;
        public string GateId => _id;

        public void ResetForRestart()
        {
            gameObject.SetActive(_index == 0);
            transform.localScale = _baseScale;
        }

        public void Initialize(int index, string id, Func<int, string, bool> onPassed)
        {
            _index = index;
            _id = id;
            _onPassed = onPassed;
            _baseScale = transform.localScale;
            gameObject.SetActive(_index == 0);
        }

        private void Update()
        {
            transform.Rotate(Vector3.forward, 12f * Time.deltaTime, Space.Self);
            transform.localScale = _baseScale * (1f + Mathf.Sin(Time.time * 3f + _index) * 0.025f);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<Gameplay.AircraftController>() == null ||
                _onPassed == null || !_onPassed(_index, _id))
            {
                return;
            }
            SignalHunt.World.SignalPickupBurst.Spawn(transform.position,
                GetComponentInChildren<Renderer>()?.sharedMaterial);
            foreach (var gate in FindObjectsByType<FlightGate>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (gate.Index == _index + 1)
                {
                    gate.gameObject.SetActive(true);
                    break;
                }
            }
            gameObject.SetActive(false);
        }
    }
}
