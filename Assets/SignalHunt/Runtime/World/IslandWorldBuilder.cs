using System;
using System.Collections.Generic;
using SignalHunt.Visual;
using UnityEngine;

namespace SignalHunt.World
{
    public static class IslandWorldBuilder
    {
        private static Mesh _pineConeMesh;

        public static GameObject Build(IslandLayout layout, GamePalette palette, Action<string> onRelicCollected)
        {
            var root = new GameObject("Daily Emerald Isle");
            PrimitiveFactory.Cube("Ocean", root.transform,
                new Vector3(0f, layout.waterLevel - 0.42f, 0f),
                new Vector3(250f, 0.8f, 250f), palette.Water, false);
            BuildTerrain(root.transform, layout, palette);
            BuildRoad(root.transform, layout, palette);
            BuildStartGate(root.transform, layout, palette);

            foreach (var ramp in layout.ramps)
            {
                BuildRamp(root.transform, ramp, palette);
            }
            foreach (var tree in layout.trees)
            {
                BuildPine(root.transform, tree, palette);
            }
            foreach (var rock in layout.rocks)
            {
                BuildRock(root.transform, rock, palette);
            }
            foreach (var relic in layout.relics)
            {
                TownWorldBuilder.BuildRelic(root.transform, relic, palette, onRelicCollected);
            }
            return root;
        }

        private static void BuildTerrain(Transform parent, IslandLayout layout, GamePalette palette)
        {
            var meshObject = new GameObject("Low Poly Island Terrain");
            meshObject.transform.SetParent(parent, false);
            var filter = meshObject.AddComponent<MeshFilter>();
            var renderer = meshObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new[]
            {
                palette.GrassMaterials[0], palette.GrassMaterials[1], palette.GrassMaterials[2], palette.GrassMaterials[3],
                palette.SandMaterials[0], palette.SandMaterials[1]
            };

            var resolution = layout.terrainResolution;
            var half = layout.worldSize * 0.5f;
            var step = layout.worldSize / (resolution - 1);
            var vertices = new List<Vector3>((resolution - 1) * (resolution - 1) * 6);
            var triangles = new List<int>[6];
            for (var material = 0; material < triangles.Length; material++)
            {
                triangles[material] = new List<int>();
            }

            for (var z = 0; z < resolution - 1; z++)
            {
                for (var x = 0; x < resolution - 1; x++)
                {
                    var x0 = -half + x * step;
                    var x1 = x0 + step;
                    var z0 = -half + z * step;
                    var z1 = z0 + step;
                    var v00 = new Vector3(x0, IslandLayoutGenerator.SampleHeight(layout, x0, z0), z0);
                    var v10 = new Vector3(x1, IslandLayoutGenerator.SampleHeight(layout, x1, z0), z0);
                    var v11 = new Vector3(x1, IslandLayoutGenerator.SampleHeight(layout, x1, z1), z1);
                    var v01 = new Vector3(x0, IslandLayoutGenerator.SampleHeight(layout, x0, z1), z1);
                    var centerRadius = new Vector2((x0 + x1) * 0.5f, (z0 + z1) * 0.5f).magnitude;
                    var materialIndex = centerRadius > 59f
                        ? 4 + ((x + z) & 1)
                        : PositiveModulo(x * 3 + z * 5 + (int)(layout.terrainSeed & 3u), 4);
                    AddTriangle(vertices, triangles[materialIndex], v00, v11, v10);
                    AddTriangle(vertices, triangles[materialIndex], v00, v01, v11);
                }
            }

            var mesh = new Mesh { name = "Procedural Emerald Isle Terrain" };
            mesh.SetVertices(vertices);
            mesh.subMeshCount = triangles.Length;
            for (var index = 0; index < triangles.Length; index++)
            {
                mesh.SetTriangles(triangles[index], index);
            }
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            filter.sharedMesh = mesh;
            meshObject.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        private static void BuildRoad(Transform parent, IslandLayout layout, GamePalette palette)
        {
            BuildRibbon("Road Shoulder", parent, layout.roadPoints, 10.4f, 0.035f, palette.RoadShoulder, false);
            BuildRibbon("Island Road", parent, layout.roadPoints, 7.3f, 0.085f, palette.IslandRoad, true);

            for (var index = 0; index < layout.roadPoints.Count; index += 5)
            {
                var point = layout.roadPoints[index];
                var next = layout.roadPoints[(index + 1) % layout.roadPoints.Count];
                var direction = next - point;
                var marker = PrimitiveFactory.Cube("Road Dash", parent, point + Vector3.up * 0.14f,
                    new Vector3(0.11f, 0.035f, Mathf.Min(2.2f, direction.magnitude * 0.65f)),
                    palette.RoadMarking, false);
                marker.transform.rotation = Quaternion.Euler(0f, Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg, 0f);
            }
        }

        private static void BuildRibbon(string name, Transform parent, IReadOnlyList<Vector3> points,
            float width, float yOffset, Material material, bool collider)
        {
            var roadObject = new GameObject(name);
            roadObject.transform.SetParent(parent, false);
            var vertices = new Vector3[points.Count * 2];
            var triangles = new int[points.Count * 6];
            for (var index = 0; index < points.Count; index++)
            {
                var previous = points[(index - 1 + points.Count) % points.Count];
                var next = points[(index + 1) % points.Count];
                var tangent = next - previous;
                tangent.y = 0f;
                tangent.Normalize();
                var normal = new Vector3(-tangent.z, 0f, tangent.x);
                vertices[index * 2] = points[index] - normal * width * 0.5f + Vector3.up * yOffset;
                vertices[index * 2 + 1] = points[index] + normal * width * 0.5f + Vector3.up * yOffset;

                var nextIndex = (index + 1) % points.Count;
                var triangle = index * 6;
                triangles[triangle] = index * 2;
                triangles[triangle + 1] = index * 2 + 1;
                triangles[triangle + 2] = nextIndex * 2;
                triangles[triangle + 3] = index * 2 + 1;
                triangles[triangle + 4] = nextIndex * 2 + 1;
                triangles[triangle + 5] = nextIndex * 2;
            }

            var mesh = new Mesh { name = name + " Mesh", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            roadObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            roadObject.AddComponent<MeshRenderer>().sharedMaterial = material;
            if (collider)
            {
                roadObject.AddComponent<MeshCollider>().sharedMesh = mesh;
            }
        }

        private static void BuildStartGate(Transform parent, IslandLayout layout, GamePalette palette)
        {
            var point = layout.roadPoints[0];
            var next = layout.roadPoints[1];
            var direction = next - point;
            var gate = new GameObject("Island Start Gate");
            gate.transform.SetParent(parent, false);
            gate.transform.position = point;
            gate.transform.rotation = Quaternion.Euler(0f, Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg, 0f);
            PrimitiveFactory.Cylinder("Start Pole Left", gate.transform, new Vector3(-4.2f, 2.4f, 0f),
                new Vector3(0.12f, 2.4f, 0.12f), palette.NeonMaterials[0], false);
            PrimitiveFactory.Cylinder("Start Pole Right", gate.transform, new Vector3(4.2f, 2.4f, 0f),
                new Vector3(0.12f, 2.4f, 0.12f), palette.NeonMaterials[1], false);
            PrimitiveFactory.Cube("Start Banner", gate.transform, new Vector3(0f, 4.55f, 0f),
                new Vector3(8.6f, 0.42f, 0.28f), palette.NeonMaterials[2], false);
            for (var stripe = -4; stripe <= 4; stripe++)
            {
                PrimitiveFactory.Cube("Start Line", gate.transform, new Vector3(stripe * 0.82f, 0.15f, 0f),
                    new Vector3(0.42f, 0.035f, 0.8f), stripe % 2 == 0 ? palette.RoadMarking : palette.DarkMetal, false);
            }
        }

        private static void BuildRamp(Transform parent, IslandRampDefinition definition, GamePalette palette)
        {
            var ramp = PrimitiveFactory.Cube("Island Jump Ramp", parent, definition.position,
                definition.scale, palette.RoadShoulder);
            ramp.transform.rotation = Quaternion.Euler(-8f, definition.yaw, 0f);
            PrimitiveFactory.Cube("Ramp Chevron", ramp.transform, new Vector3(0f, 0.54f, 0.06f),
                new Vector3(0.72f, 0.055f, 0.78f), palette.NeonMaterials[definition.styleIndex % 4], false);
        }

        private static void BuildPine(Transform parent, IslandTreeDefinition definition, GamePalette palette)
        {
            var root = new GameObject("Low Poly Pine");
            root.transform.SetParent(parent, false);
            root.transform.position = definition.position;
            var scale = definition.scale;
            PrimitiveFactory.Cylinder("Pine Trunk", root.transform, new Vector3(0f, 1.05f * scale, 0f),
                new Vector3(0.22f * scale, 1.05f * scale, 0.22f * scale), palette.TreeTrunk, false);
            CreateCone("Pine Crown Lower", root.transform, new Vector3(0f, 2.65f * scale, 0f),
                new Vector3(2.25f * scale, 3.5f * scale, 2.25f * scale),
                palette.PineMaterials[definition.styleIndex % palette.PineMaterials.Length]);
            CreateCone("Pine Crown Upper", root.transform, new Vector3(0f, 4.0f * scale, 0f),
                new Vector3(1.55f * scale, 2.8f * scale, 1.55f * scale),
                palette.PineMaterials[(definition.styleIndex + 1) % palette.PineMaterials.Length]);
            var collider = root.AddComponent<CapsuleCollider>();
            collider.radius = 0.48f * scale;
            collider.height = 3.5f * scale;
            collider.center = Vector3.up * (1.75f * scale);
        }

        private static void BuildRock(Transform parent, IslandRockDefinition definition, GamePalette palette)
        {
            var rock = PrimitiveFactory.Cube("Island Rock", parent, definition.position, definition.scale,
                palette.RockMaterials[definition.styleIndex % palette.RockMaterials.Length]);
            rock.transform.rotation = Quaternion.Euler(definition.yaw * 0.08f, definition.yaw, definition.yaw * 0.04f);
        }

        private static void CreateCone(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            if (_pineConeMesh == null)
            {
                _pineConeMesh = CreateConeMesh();
            }
            var cone = new GameObject(name);
            cone.transform.SetParent(parent, false);
            cone.transform.localPosition = position;
            cone.transform.localScale = scale;
            cone.AddComponent<MeshFilter>().sharedMesh = _pineConeMesh;
            cone.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static Mesh CreateConeMesh()
        {
            const int sides = 7;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (var side = 0; side < sides; side++)
            {
                var next = (side + 1) % sides;
                var a = new Vector3(Mathf.Cos(side * Mathf.PI * 2f / sides) * 0.5f, -0.5f,
                    Mathf.Sin(side * Mathf.PI * 2f / sides) * 0.5f);
                var b = new Vector3(Mathf.Cos(next * Mathf.PI * 2f / sides) * 0.5f, -0.5f,
                    Mathf.Sin(next * Mathf.PI * 2f / sides) * 0.5f);
                AddTriangle(vertices, triangles, Vector3.up * 0.5f, a, b);
                AddTriangle(vertices, triangles, Vector3.down * 0.5f, b, a);
            }
            var mesh = new Mesh { name = "Seven Sided Pine Cone" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddTriangle(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c)
        {
            var start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
        }

        private static int PositiveModulo(int value, int modulus)
        {
            return (value % modulus + modulus) % modulus;
        }
    }
}
