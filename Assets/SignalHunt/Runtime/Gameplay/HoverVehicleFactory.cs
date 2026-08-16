using SignalHunt.Visual;
using UnityEngine;

namespace SignalHunt.Gameplay
{
    public static class HoverVehicleFactory
    {
        public static HoverVehicleController CreatePlayer(GamePalette palette, Vector3 spawn, float heading)
        {
            var root = new GameObject("Player Hover Car");
            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.8f, 0.75f, 3.6f);
            root.AddComponent<Rigidbody>();
            BuildVisual(root.transform, palette.HoverBody, palette.HoverGlass, palette.NeonMaterials[1]);
            var controller = root.AddComponent<HoverVehicleController>();
            controller.Initialize(spawn, heading);
            return controller;
        }

        public static GameObject CreateReplayVisual(GamePalette palette, string name, bool ghost)
        {
            var root = new GameObject(name);
            var body = ghost ? palette.Ghost : palette.HoverBody;
            BuildVisual(root.transform, body, ghost ? palette.Ghost : palette.HoverGlass, body);
            return root;
        }

        private static void BuildVisual(Transform root, Material body, Material glass, Material accent)
        {
            PrimitiveFactory.Cube("Chassis", root, new Vector3(0f, 0f, 0f),
                new Vector3(1.8f, 0.45f, 3.5f), body, false);
            var nose = PrimitiveFactory.Cube("Nose", root, new Vector3(0f, -0.03f, 1.45f),
                new Vector3(1.55f, 0.3f, 1.05f), body, false);
            nose.transform.localRotation = Quaternion.Euler(7f, 0f, 0f);
            PrimitiveFactory.Cube("Cockpit", root, new Vector3(0f, 0.42f, 0.25f),
                new Vector3(1.32f, 0.55f, 1.5f), glass, false);
            PrimitiveFactory.Cube("Left Rail", root, new Vector3(-1.05f, -0.1f, 0f),
                new Vector3(0.18f, 0.2f, 3f), accent, false);
            PrimitiveFactory.Cube("Right Rail", root, new Vector3(1.05f, -0.1f, 0f),
                new Vector3(0.18f, 0.2f, 3f), accent, false);
            PrimitiveFactory.Cube("Rear Signal", root, new Vector3(0f, 0f, -1.8f),
                new Vector3(1.25f, 0.12f, 0.12f), accent, false);
        }
    }
}
