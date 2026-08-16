using System;
using System.Collections.Generic;
using SignalHunt.Core;
using UnityEngine;

namespace SignalHunt.World
{
    public static class IslandLayoutGenerator
    {
        private const float IslandRadius = 72f;

        public static IslandLayout Generate(DailyChallenge challenge)
        {
            if (challenge == null)
            {
                throw new ArgumentNullException(nameof(challenge));
            }
            if (challenge.Stage != WorldStage.Island)
            {
                throw new ArgumentException("The island generator requires an island challenge.", nameof(challenge));
            }

            var random = new DeterministicRandom(challenge.generationSeed);
            var layout = new IslandLayout
            {
                challengeId = challenge.challengeId,
                terrainSeed = challenge.generationSeed ^ 0xA17E5EEDu,
                worldSize = 176f,
                waterLevel = -0.55f,
                terrainResolution = 37
            };

            GenerateHills(layout, random);
            GenerateRoad(layout, random);
            GenerateRamps(layout, random);
            GenerateRelics(layout, challenge.collectibleCount, random);
            GenerateTrees(layout, random);
            GenerateRocks(layout, random);
            return layout;
        }

        public static float SampleHeight(IslandLayout layout, float x, float z)
        {
            var radius = Mathf.Sqrt(x * x + z * z);
            var shore = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((IslandRadius - radius) / 12f));
            var height = Mathf.Lerp(-2.8f, 0.65f, shore);
            height += Mathf.Clamp01((58f - radius) / 58f) * 0.9f;
            height += ValueNoise(x, z, layout.terrainSeed) * shore * 0.72f;

            foreach (var hill in layout.hills)
            {
                var dx = x - hill.center.x;
                var dz = z - hill.center.y;
                var normalized = (dx * dx + dz * dz) / (hill.radius * hill.radius);
                height += Mathf.Exp(-normalized * 2.2f) * hill.height * shore;
            }

            return height;
        }

        public static float DistanceToRoad(IReadOnlyList<Vector3> roadPoints, Vector3 point)
        {
            var best = float.MaxValue;
            for (var index = 0; index < roadPoints.Count; index++)
            {
                var start = roadPoints[index];
                var end = roadPoints[(index + 1) % roadPoints.Count];
                start.y = end.y = point.y = 0f;
                var segment = end - start;
                var denominator = Mathf.Max(0.0001f, segment.sqrMagnitude);
                var progress = Mathf.Clamp01(Vector3.Dot(point - start, segment) / denominator);
                best = Mathf.Min(best, Vector3.Distance(point, start + segment * progress));
            }
            return best;
        }

        private static void GenerateHills(IslandLayout layout, DeterministicRandom random)
        {
            for (var index = 0; index < 8; index++)
            {
                var angle = random.Range(0f, Mathf.PI * 2f);
                var distance = random.Range(5f, 43f);
                layout.hills.Add(new IslandHillDefinition
                {
                    center = new Vector2(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance),
                    radius = random.Range(14f, 28f),
                    height = random.Range(1.4f, 4.8f)
                });
            }
        }

        private static void GenerateRoad(IslandLayout layout, DeterministicRandom random)
        {
            const int controlCount = 12;
            const int samplesPerCurve = 7;
            var controls = new List<Vector3>(controlCount);
            for (var index = 0; index < controlCount; index++)
            {
                var angle = -Mathf.PI * 0.5f + index * Mathf.PI * 2f / controlCount;
                var radius = index == 0 ? 51f : random.Range(35f, 54f);
                var x = Mathf.Cos(angle) * radius;
                var z = Mathf.Sin(angle) * radius;
                controls.Add(new Vector3(x, SampleHeight(layout, x, z) + 0.18f, z));
            }

            for (var index = 0; index < controlCount; index++)
            {
                var p0 = controls[(index - 1 + controlCount) % controlCount];
                var p1 = controls[index];
                var p2 = controls[(index + 1) % controlCount];
                var p3 = controls[(index + 2) % controlCount];
                for (var sample = 0; sample < samplesPerCurve; sample++)
                {
                    var t = sample / (float)samplesPerCurve;
                    var point = CatmullRom(p0, p1, p2, p3, t);
                    point.y = SampleHeight(layout, point.x, point.z) + 0.2f;
                    layout.roadPoints.Add(point);
                }
            }

            var spawn = layout.roadPoints[0];
            var forward = layout.roadPoints[2] - spawn;
            layout.playerSpawn = spawn + Vector3.up * 1.15f;
            layout.playerHeading = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        }

        private static void GenerateRamps(IslandLayout layout, DeterministicRandom random)
        {
            var indices = new[] { 15, 39, 64 };
            foreach (var index in indices)
            {
                var point = layout.roadPoints[index % layout.roadPoints.Count];
                var next = layout.roadPoints[(index + 1) % layout.roadPoints.Count];
                var direction = next - point;
                layout.ramps.Add(new IslandRampDefinition
                {
                    position = point + Vector3.up * 0.24f,
                    scale = new Vector3(5.6f, 0.72f, 7.2f),
                    yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg,
                    styleIndex = random.Range(0, 4)
                });
            }
        }

        private static void GenerateRelics(IslandLayout layout, int count, DeterministicRandom random)
        {
            for (var index = 0; index < count; index++)
            {
                const int spawnClearanceSamples = 10;
                var usableSamples = layout.roadPoints.Count - spawnClearanceSamples * 2;
                var roadIndex = spawnClearanceSamples + (index + 1) * usableSamples / (count + 1);
                var point = layout.roadPoints[roadIndex];
                var next = layout.roadPoints[(roadIndex + 1) % layout.roadPoints.Count];
                var tangent = (next - point).normalized;
                var normal = new Vector3(-tangent.z, 0f, tangent.x);
                var offset = index % 3 == 0 ? random.Range(7f, 11f) * (index % 2 == 0 ? 1f : -1f) : random.Range(-2.2f, 2.2f);
                var position = point + normal * offset;
                position.y = SampleHeight(layout, position.x, position.z) + 1.25f;
                layout.relics.Add(new RelicDefinition
                {
                    id = $"relic-{index + 1:D2}",
                    position = position,
                    styleIndex = random.Range(0, 4),
                    bonus = index == count - 1
                });
            }
        }

        private static void GenerateTrees(IslandLayout layout, DeterministicRandom random)
        {
            var attempts = 0;
            while (layout.trees.Count < 86 && attempts++ < 1200)
            {
                var angle = random.Range(0f, Mathf.PI * 2f);
                var distance = Mathf.Sqrt(random.NextFloat()) * 65f;
                var position = new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
                if (DistanceToRoad(layout.roadPoints, position) < 6.3f ||
                    Vector3.Distance(position, layout.playerSpawn) < 10f ||
                    layout.relics.Exists(relic => Vector3.Distance(position, relic.position) < 4f))
                {
                    continue;
                }

                position.y = SampleHeight(layout, position.x, position.z);
                layout.trees.Add(new IslandTreeDefinition
                {
                    position = position,
                    scale = random.Range(0.72f, 1.45f),
                    styleIndex = random.Range(0, 3)
                });
            }
        }

        private static void GenerateRocks(IslandLayout layout, DeterministicRandom random)
        {
            var attempts = 0;
            while (layout.rocks.Count < 28 && attempts++ < 500)
            {
                var angle = random.Range(0f, Mathf.PI * 2f);
                var distance = Mathf.Sqrt(random.NextFloat()) * 67f;
                var position = new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
                if (DistanceToRoad(layout.roadPoints, position) < 4.8f)
                {
                    continue;
                }
                position.y = SampleHeight(layout, position.x, position.z) + 0.35f;
                layout.rocks.Add(new IslandRockDefinition
                {
                    position = position,
                    scale = new Vector3(random.Range(0.5f, 1.6f), random.Range(0.5f, 1.3f), random.Range(0.5f, 1.6f)),
                    yaw = random.Range(0f, 360f),
                    styleIndex = random.Range(0, 3)
                });
            }
        }

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            var t2 = t * t;
            var t3 = t2 * t;
            return 0.5f * ((2f * p1) + (-p0 + p2) * t +
                           (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                           (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        private static float ValueNoise(float x, float z, uint seed)
        {
            const float cellSize = 8f;
            var cellX = Mathf.FloorToInt(x / cellSize);
            var cellZ = Mathf.FloorToInt(z / cellSize);
            var tx = Mathf.SmoothStep(0f, 1f, x / cellSize - cellX);
            var tz = Mathf.SmoothStep(0f, 1f, z / cellSize - cellZ);
            var a = HashValue(cellX, cellZ, seed);
            var b = HashValue(cellX + 1, cellZ, seed);
            var c = HashValue(cellX, cellZ + 1, seed);
            var d = HashValue(cellX + 1, cellZ + 1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), tz) - 0.5f;
        }

        private static float HashValue(int x, int z, uint seed)
        {
            unchecked
            {
                var value = (uint)x * 0x8DA6B343u ^ (uint)z * 0xD8163841u ^ seed;
                value ^= value >> 13;
                value *= 0x85EBCA6Bu;
                value ^= value >> 16;
                return (value & 0x00FFFFFFu) / 16777216f;
            }
        }
    }
}
