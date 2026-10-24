using System;
using System.Collections.Generic;
using SignalHunt.Core;
using UnityEngine;

namespace WaypointWings.World
{
    [Serializable]
    public sealed class FlightCourseLayout
    {
        public string challengeId;
        public Vector3 playerSpawn;
        public Quaternion playerRotation;
        public List<FlightGateDefinition> gates = new();
        public List<SkyIslandDefinition> islands = new();
        public List<CloudDefinition> clouds = new();
        public List<SkyTowerDefinition> towers = new();
    }

    [Serializable]
    public struct FlightGateDefinition
    {
        public string id;
        public Vector3 position;
        public Quaternion rotation;
        public float radius;
        public int styleIndex;
    }

    [Serializable]
    public struct SkyIslandDefinition
    {
        public Vector3 position;
        public Vector3 scale;
        public int styleIndex;
    }

    [Serializable]
    public struct CloudDefinition
    {
        public Vector3 position;
        public Vector3 scale;
    }

    [Serializable]
    public struct SkyTowerDefinition
    {
        public Vector3 position;
        public Vector3 scale;
        public int styleIndex;
    }

    public static class FlightCourseGenerator
    {
        public static FlightCourseLayout Generate(DailyChallenge challenge)
        {
            if (challenge == null || challenge.appKey != DailyGameCatalog.WaypointWingsAppKey)
            {
                throw new ArgumentException("A Waypoint Wings challenge is required.", nameof(challenge));
            }

            var random = new DeterministicRandom(challenge.generationSeed);
            var layout = new FlightCourseLayout { challengeId = challenge.challengeId };
            var phase = random.Range(0f, Mathf.PI * 2f);
            for (var index = 0; index < challenge.collectibleCount; index++)
            {
                var progress = index / (float)Mathf.Max(1, challenge.collectibleCount - 1);
                var z = -82f + index * 28f;
                var x = Mathf.Sin(phase + progress * Mathf.PI * 2f) * 18f + random.Range(-2f, 2f);
                var y = 28f + Mathf.Sin(phase * 0.5f + progress * Mathf.PI * 2f) * 5f + random.Range(-1f, 1f);
                var position = new Vector3(x, Mathf.Clamp(y, 20f, 36f), z);
                layout.gates.Add(new FlightGateDefinition
                {
                    id = $"gate-{index + 1:D2}",
                    position = position,
                    radius = random.Range(9f, 10.5f),
                    styleIndex = index % 4
                });
            }

            for (var index = 0; index < layout.gates.Count; index++)
            {
                var previous = index == 0 ? new Vector3(0f, 20f, -110f) : layout.gates[index - 1].position;
                var next = index == layout.gates.Count - 1
                    ? layout.gates[index].position + (layout.gates[index].position - previous).normalized * 24f
                    : layout.gates[index + 1].position;
                var gate = layout.gates[index];
                gate.rotation = Quaternion.LookRotation((next - previous).normalized, Vector3.up);
                layout.gates[index] = gate;
            }

            var first = layout.gates[0];
            var approach = first.rotation * Vector3.forward;
            layout.playerSpawn = first.position - approach * 28f;
            layout.playerRotation = first.rotation;
            GenerateEnvironment(layout, challenge, random);
            return layout;
        }

        private static void GenerateEnvironment(FlightCourseLayout layout, DailyChallenge challenge, DeterministicRandom random)
        {
            var archipelago = challenge.stageKey == "archipelago";
            for (var index = 0; index < 14; index++)
            {
                var gate = layout.gates[random.Range(0, layout.gates.Count)];
                var side = random.NextFloat() < 0.5f ? -1f : 1f;
                var position = gate.position + new Vector3(side * random.Range(32f, 48f),
                    -gate.position.y + random.Range(0f, 4f), random.Range(-10f, 10f));
                if (archipelago)
                {
                    layout.islands.Add(new SkyIslandDefinition
                    {
                        position = position,
                        scale = new Vector3(random.Range(12f, 22f), random.Range(2.2f, 5f), random.Range(12f, 22f)),
                        styleIndex = random.Range(0, 4)
                    });
                }
                else
                {
                    layout.towers.Add(new SkyTowerDefinition
                    {
                        position = position + Vector3.up * random.Range(7f, 18f),
                        scale = new Vector3(random.Range(5f, 11f), random.Range(14f, 38f), random.Range(5f, 11f)),
                        styleIndex = random.Range(0, 4)
                    });
                }
            }

            for (var index = 0; index < 20; index++)
            {
                var gate = layout.gates[random.Range(0, layout.gates.Count)];
                layout.clouds.Add(new CloudDefinition
                {
                    position = gate.position + new Vector3(random.Range(-70f, 70f), random.Range(-18f, 19f), random.Range(-32f, 32f)),
                    scale = new Vector3(random.Range(5f, 14f), random.Range(1.5f, 4.2f), random.Range(4f, 11f))
                });
            }
        }
    }
}
