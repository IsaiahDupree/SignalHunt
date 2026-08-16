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
            var visualRoot = new GameObject("Vehicle Visual").transform;
            visualRoot.SetParent(root.transform, false);
            BuildVisual(visualRoot, palette.HoverBody, palette.HoverGlass, palette.NeonMaterials[1], palette.VehicleTrail, true);
            var controller = root.AddComponent<HoverVehicleController>();
            controller.Initialize(spawn, heading);
            root.AddComponent<HoverVehicleVisualFx>().Initialize(visualRoot, controller);
            return controller;
        }

        public static GameObject CreateReplayVisual(GamePalette palette, string name, bool ghost, int colorIndex = -1)
        {
            var root = new GameObject(name);
            var body = ghost ? palette.Ghost : colorIndex >= 0 ? palette.CreateReplayVehicleMaterial(colorIndex) : palette.HoverBody;
            var accent = ghost ? palette.Ghost : colorIndex >= 0 ? palette.NeonMaterials[(colorIndex + 1) % 4] : body;
            BuildVisual(root.transform, body, ghost ? palette.Ghost : palette.HoverGlass, accent, palette.VehicleTrail, !ghost);
            if (!ghost)
            {
                foreach (var trail in root.GetComponentsInChildren<TrailRenderer>())
                {
                    trail.Clear();
                    trail.emitting = true;
                }
            }
            return root;
        }

        private static void BuildVisual(Transform root, Material body, Material glass, Material accent, Material trailMaterial, bool trails)
        {
            PrimitiveFactory.Cube("Lower Chassis", root, new Vector3(0f, -0.08f, -0.05f),
                new Vector3(1.72f, 0.34f, 3.6f), body, false);
            var upperChassis = PrimitiveFactory.Cube("Upper Chassis", root, new Vector3(0f, 0.16f, 0.18f),
                new Vector3(1.5f, 0.28f, 2.85f), body, false);
            upperChassis.transform.localRotation = Quaternion.Euler(-2f, 0f, 0f);

            var nose = PrimitiveFactory.Cube("Arrow Nose", root, new Vector3(0f, 0.04f, 1.68f),
                new Vector3(1.18f, 0.22f, 0.95f), body, false);
            nose.transform.localRotation = Quaternion.Euler(11f, 0f, 0f);
            var leftBlade = PrimitiveFactory.Cube("Left Nose Blade", root, new Vector3(-0.72f, -0.04f, 1.33f),
                new Vector3(0.28f, 0.18f, 1.28f), accent, false);
            leftBlade.transform.localRotation = Quaternion.Euler(4f, -8f, 0f);
            var rightBlade = PrimitiveFactory.Cube("Right Nose Blade", root, new Vector3(0.72f, -0.04f, 1.33f),
                new Vector3(0.28f, 0.18f, 1.28f), accent, false);
            rightBlade.transform.localRotation = Quaternion.Euler(4f, 8f, 0f);

            var cockpit = PrimitiveFactory.Cube("Cockpit", root, new Vector3(0f, 0.43f, 0.22f),
                new Vector3(1.18f, 0.52f, 1.42f), glass, false);
            cockpit.transform.localRotation = Quaternion.Euler(-5f, 0f, 0f);
            PrimitiveFactory.Cube("Cockpit Stripe", root, new Vector3(0f, 0.50f, 0.96f),
                new Vector3(0.92f, 0.08f, 0.08f), accent, false);

            PrimitiveFactory.Cube("Left Rail", root, new Vector3(-1.02f, -0.08f, -0.08f),
                new Vector3(0.18f, 0.24f, 3.18f), accent, false);
            PrimitiveFactory.Cube("Right Rail", root, new Vector3(1.02f, -0.08f, -0.08f),
                new Vector3(0.18f, 0.24f, 3.18f), accent, false);
            PrimitiveFactory.Sphere("Left Hover Pod", root, new Vector3(-1.03f, -0.18f, -0.78f),
                new Vector3(0.42f, 0.22f, 0.72f), accent, false);
            PrimitiveFactory.Sphere("Right Hover Pod", root, new Vector3(1.03f, -0.18f, -0.78f),
                new Vector3(0.42f, 0.22f, 0.72f), accent, false);
            PrimitiveFactory.Sphere("Hover Glow", root, new Vector3(0f, -0.34f, -0.1f),
                new Vector3(1.55f, 0.08f, 2.5f), accent, false);
            PrimitiveFactory.Cube("Rear Signal", root, new Vector3(0f, 0.02f, -1.83f),
                new Vector3(1.3f, 0.13f, 0.10f), accent, false);
            PrimitiveFactory.Cube("Left Fin", root, new Vector3(-0.76f, 0.27f, -1.25f),
                new Vector3(0.12f, 0.56f, 0.92f), body, false);
            PrimitiveFactory.Cube("Right Fin", root, new Vector3(0.76f, 0.27f, -1.25f),
                new Vector3(0.12f, 0.56f, 0.92f), body, false);

            if (trails)
            {
                CreateTrail(root, new Vector3(-0.78f, -0.12f, -1.76f), trailMaterial);
                CreateTrail(root, new Vector3(0.78f, -0.12f, -1.76f), trailMaterial);
            }
        }

        private static void CreateTrail(Transform parent, Vector3 localPosition, Material material)
        {
            var trailObject = new GameObject("Signal Trail");
            trailObject.transform.SetParent(parent, false);
            trailObject.transform.localPosition = localPosition;
            var trail = trailObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = material;
            trail.time = 0.16f;
            trail.startWidth = 0.08f;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.08f;
            trail.numCornerVertices = 3;
            trail.alignment = LineAlignment.View;
            trail.startColor = Color.white;
            trail.endColor = new Color(1f, 1f, 1f, 0f);
            trail.emitting = false;
        }
    }
}
