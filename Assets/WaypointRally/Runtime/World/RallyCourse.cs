using System;
using System.Collections.Generic;
using SignalHunt.Core;
using UnityEngine;

namespace WaypointRally.World
{
    [Serializable]
    public sealed class RallyCourseLayout
    {
        public string challengeId;
        public Vector3 playerSpawn;
        public float playerHeading;
        public List<RallyCheckpointDefinition> checkpoints = new();
        public List<RallyRoadDefinition> roads = new();
        public List<RallyPropDefinition> buildings = new();
        public List<RallyPropDefinition> obstacles = new();
        public List<RallyPropDefinition> decorations = new();
        public List<RallyRampDefinition> ramps = new();
    }

    [Serializable]
    public struct RallyCheckpointDefinition
    {
        public string id;
        public Vector3 position;
        public float heading;
        public float width;
        public int styleIndex;
    }

    [Serializable]
    public struct RallyRoadDefinition
    {
        public Vector3 start;
        public Vector3 end;
        public float width;
        public int styleIndex;
    }

    [Serializable]
    public struct RallyPropDefinition
    {
        public Vector3 position;
        public Vector3 scale;
        public float heading;
        public int styleIndex;
    }

    [Serializable]
    public struct RallyRampDefinition
    {
        public Vector3 position;
        public float heading;
        public Vector3 scale;
    }

    public static class RallyCourseGenerator
    {
        private static readonly Vector2[] TownRoute =
        {
            new(-58f, -54f), new(-18f, -58f), new(24f, -54f), new(58f, -18f), new(54f, 24f),
            new(20f, 58f), new(-22f, 54f), new(-58f, 22f), new(-20f, 10f), new(20f, -18f)
        };

        public static RallyCourseLayout Generate(DailyChallenge challenge)
        {
            if (challenge == null || challenge.appKey != DailyGameCatalog.WaypointRallyAppKey)
            {
                throw new ArgumentException("A Waypoint Rally challenge is required.", nameof(challenge));
            }

            var random = new DeterministicRandom(challenge.generationSeed);
            var layout = new RallyCourseLayout { challengeId = challenge.challengeId };
            if (challenge.stageKey == "harbor-town")
            {
                GenerateTown(layout, random);
            }
            else
            {
                GenerateDustlands(layout, random);
            }
            OrientCheckpointsAndSpawn(layout);
            return layout;
        }

        private static void GenerateTown(RallyCourseLayout layout, DeterministicRandom random)
        {
            for (var lane = -1; lane <= 1; lane++)
            {
                var offset = lane * 40f;
                layout.roads.Add(new RallyRoadDefinition
                    { start = new Vector3(offset, 0f, -82f), end = new Vector3(offset, 0f, 82f), width = 11f, styleIndex = 0 });
                layout.roads.Add(new RallyRoadDefinition
                    { start = new Vector3(-82f, 0f, offset), end = new Vector3(82f, 0f, offset), width = 11f, styleIndex = 0 });
            }
            layout.roads.Add(new RallyRoadDefinition
                { start = new Vector3(-70f, 0f, -70f), end = new Vector3(70f, 0f, 70f), width = 7f, styleIndex = 1 });

            for (var index = 0; index < TownRoute.Length; index++)
            {
                var point = TownRoute[index];
                layout.checkpoints.Add(new RallyCheckpointDefinition
                {
                    id = $"checkpoint-{index + 1:D2}",
                    position = new Vector3(point.x + random.Range(-3f, 3f), 0.65f, point.y + random.Range(-3f, 3f)),
                    width = random.Range(8f, 11f),
                    styleIndex = index % 4
                });
            }

            for (var x = -2; x <= 2; x++)
            {
                for (var z = -2; z <= 2; z++)
                {
                    if (Mathf.Abs(x) <= 1 && Mathf.Abs(z) <= 1 || random.NextFloat() < 0.22f)
                    {
                        continue;
                    }
                    layout.buildings.Add(new RallyPropDefinition
                    {
                        position = new Vector3(x * 39f + random.Range(-4f, 4f), 0f, z * 39f + random.Range(-4f, 4f)),
                        scale = new Vector3(random.Range(10f, 17f), random.Range(8f, 24f), random.Range(10f, 17f)),
                        heading = random.Range(-8f, 8f),
                        styleIndex = random.Range(0, 4)
                    });
                }
            }

            for (var index = 0; index < 28; index++)
            {
                var angle = random.Range(0f, Mathf.PI * 2f);
                var radius = random.Range(52f, 90f);
                layout.decorations.Add(new RallyPropDefinition
                {
                    position = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius),
                    scale = new Vector3(random.Range(0.5f, 1.1f), random.Range(2.2f, 4.8f), random.Range(0.5f, 1.1f)),
                    heading = random.Range(0f, 360f),
                    styleIndex = random.Range(0, 4)
                });
            }
            layout.ramps.Add(new RallyRampDefinition { position = new Vector3(0f, 0.6f, 9f), heading = 45f, scale = new Vector3(7f, 1.1f, 10f) });
            layout.ramps.Add(new RallyRampDefinition { position = new Vector3(39f, 0.6f, -38f), heading = 0f, scale = new Vector3(6f, 1f, 9f) });
        }

        private static void GenerateDustlands(RallyCourseLayout layout, DeterministicRandom random)
        {
            var phase = random.Range(0f, Mathf.PI * 2f);
            for (var index = 0; index < 10; index++)
            {
                var angle = phase + index * Mathf.PI * 2f / 10f;
                var radius = index % 2 == 0 ? random.Range(58f, 72f) : random.Range(38f, 52f);
                layout.checkpoints.Add(new RallyCheckpointDefinition
                {
                    id = $"checkpoint-{index + 1:D2}",
                    position = new Vector3(Mathf.Cos(angle) * radius, 0.65f, Mathf.Sin(angle) * radius),
                    width = random.Range(9f, 13f),
                    styleIndex = index % 4
                });
            }
            for (var index = 0; index < layout.checkpoints.Count; index++)
            {
                var start = layout.checkpoints[index].position;
                var end = layout.checkpoints[(index + 1) % layout.checkpoints.Count].position;
                layout.roads.Add(new RallyRoadDefinition { start = start, end = end, width = 9f, styleIndex = 2 });
            }
            for (var index = 0; index < 42; index++)
            {
                var angle = random.Range(0f, Mathf.PI * 2f);
                var radius = random.Range(18f, 100f);
                var position = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                layout.obstacles.Add(new RallyPropDefinition
                {
                    position = position,
                    scale = new Vector3(random.Range(0.8f, 3.4f), random.Range(0.7f, 3.8f), random.Range(0.8f, 3.4f)),
                    heading = random.Range(0f, 360f),
                    styleIndex = random.Range(0, 4)
                });
            }
            for (var index = 0; index < 24; index++)
            {
                var angle = random.Range(0f, Mathf.PI * 2f);
                var radius = random.Range(30f, 105f);
                layout.decorations.Add(new RallyPropDefinition
                {
                    position = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius),
                    scale = new Vector3(random.Range(0.4f, 0.9f), random.Range(1.8f, 4.2f), random.Range(0.4f, 0.9f)),
                    heading = random.Range(0f, 360f),
                    styleIndex = random.Range(0, 4)
                });
            }
            layout.ramps.Add(new RallyRampDefinition { position = Vector3.zero + Vector3.up * 0.6f, heading = random.Range(0f, 360f), scale = new Vector3(8f, 1.4f, 12f) });
            layout.ramps.Add(new RallyRampDefinition { position = new Vector3(-32f, 0.55f, 22f), heading = 125f, scale = new Vector3(6f, 1f, 9f) });
        }

        private static void OrientCheckpointsAndSpawn(RallyCourseLayout layout)
        {
            for (var index = 0; index < layout.checkpoints.Count; index++)
            {
                var checkpoint = layout.checkpoints[index];
                var next = layout.checkpoints[(index + 1) % layout.checkpoints.Count].position;
                checkpoint.heading = Quaternion.LookRotation((next - checkpoint.position).normalized, Vector3.up).eulerAngles.y;
                layout.checkpoints[index] = checkpoint;
            }
            var first = layout.checkpoints[0];
            var forward = Quaternion.Euler(0f, first.heading, 0f) * Vector3.forward;
            layout.playerSpawn = first.position - forward * 15f + Vector3.up * 0.6f;
            layout.playerHeading = first.heading;
        }
    }
}
