using System;
using System.Collections.Generic;
using SignalHunt.Core;
using UnityEngine;

namespace SignalHunt.World
{
    public static class TownLayoutGenerator
    {
        private const int BlocksPerAxis = 4;
        private const float BlockPitch = 24f;
        private const float BlockFootprint = 16f;
        private const float WorldSize = 112f;

        public static TownLayout Generate(DailyChallenge challenge)
        {
            if (challenge == null)
            {
                throw new ArgumentNullException(nameof(challenge));
            }

            var random = new DeterministicRandom(challenge.generationSeed);
            var layout = new TownLayout
            {
                challengeId = challenge.challengeId,
                worldSize = WorldSize,
                playerSpawn = new Vector3(0f, 1.1f, -44f),
                playerHeading = 0f
            };

            GenerateBlocks(layout, random);
            GenerateStreetProps(layout, random);
            GenerateRelics(layout, challenge.collectibleCount, random);
            return layout;
        }

        private static void GenerateBlocks(TownLayout layout, DeterministicRandom random)
        {
            var origin = -(BlocksPerAxis - 1) * BlockPitch * 0.5f;
            for (var x = 0; x < BlocksPerAxis; x++)
            {
                for (var z = 0; z < BlocksPerAxis; z++)
                {
                    var center = new Vector3(origin + x * BlockPitch, 0f, origin + z * BlockPitch);
                    var isPlaza = (x == 1 && z == 1) || random.Chance(0.12f);
                    if (isPlaza)
                    {
                        AddPlaza(layout, center, random);
                        continue;
                    }

                    var splitAlongX = random.Chance(0.5f);
                    var buildingCount = random.Chance(0.36f) ? 2 : 1;
                    for (var index = 0; index < buildingCount; index++)
                    {
                        var width = buildingCount == 1 ? random.Range(11f, 15f) : random.Range(6f, 7.4f);
                        var depth = buildingCount == 1 ? random.Range(11f, 15f) : random.Range(11f, 15f);
                        if (!splitAlongX && buildingCount > 1)
                        {
                            (width, depth) = (depth, width);
                        }

                        var offset = buildingCount == 1
                            ? Vector3.zero
                            : splitAlongX
                                ? new Vector3(index == 0 ? -4.2f : 4.2f, 0f, 0f)
                                : new Vector3(0f, 0f, index == 0 ? -4.2f : 4.2f);
                        var height = random.Range(6f, 22f);
                        layout.buildings.Add(new BuildingDefinition
                        {
                            position = center + offset + Vector3.up * height * 0.5f,
                            size = new Vector3(width, height, depth),
                            materialIndex = random.Range(0, 4),
                            isLandmark = height > 18f
                        });
                    }
                }
            }
        }

        private static void AddPlaza(TownLayout layout, Vector3 center, DeterministicRandom random)
        {
            layout.props.Add(new PropDefinition
            {
                kind = TownPropKind.SignalTower,
                position = center,
                scale = new Vector3(1.2f, random.Range(6f, 10f), 1.2f),
                yaw = random.Range(0f, 360f),
                materialIndex = random.Range(0, 3)
            });

            for (var corner = 0; corner < 4; corner++)
            {
                var x = corner % 2 == 0 ? -6f : 6f;
                var z = corner < 2 ? -6f : 6f;
                layout.props.Add(new PropDefinition
                {
                    kind = TownPropKind.LightPylon,
                    position = center + new Vector3(x, 0f, z),
                    scale = new Vector3(0.3f, 3f, 0.3f),
                    yaw = 0f,
                    materialIndex = corner % 3
                });
            }
        }

        private static void GenerateStreetProps(TownLayout layout, DeterministicRandom random)
        {
            var laneCenters = new[] { -48f, -24f, 0f, 24f, 48f };
            for (var index = 0; index < 5; index++)
            {
                var horizontal = index % 2 == 0;
                var lane = laneCenters[random.Range(0, laneCenters.Length)];
                var along = random.Range(-38f, 38f);
                layout.props.Add(new PropDefinition
                {
                    kind = index < 3 ? TownPropKind.Ramp : TownPropKind.TunnelGate,
                    position = horizontal ? new Vector3(along, 0.3f, lane) : new Vector3(lane, 0.3f, along),
                    scale = index < 3 ? new Vector3(5f, 0.8f, 8f) : new Vector3(8f, 4f, 1f),
                    yaw = horizontal ? 0f : 90f,
                    materialIndex = random.Range(0, 3)
                });
            }
        }

        private static void GenerateRelics(TownLayout layout, int count, DeterministicRandom random)
        {
            var laneCenters = new[] { -48f, -24f, 0f, 24f, 48f };
            var candidates = new List<Vector3>(60);
            foreach (var lane in laneCenters)
            {
                for (var step = -2; step <= 2; step++)
                {
                    var along = step * 20f + random.Range(-3f, 3f);
                    candidates.Add(new Vector3(along, 1.15f, lane + random.Range(-2.2f, 2.2f)));
                    candidates.Add(new Vector3(lane + random.Range(-2.2f, 2.2f), 1.15f, along));
                }
            }

            Shuffle(candidates, random);
            var selected = new List<Vector3>(count);
            foreach (var candidate in candidates)
            {
                if (Vector3.Distance(candidate, layout.playerSpawn) < 12f || IsInsideBuilding(candidate, layout.buildings))
                {
                    continue;
                }

                var tooClose = selected.Exists(position => Vector3.Distance(position, candidate) < 10f);
                if (tooClose)
                {
                    continue;
                }

                selected.Add(candidate);
                if (selected.Count == count)
                {
                    break;
                }
            }

            if (selected.Count != count)
            {
                throw new InvalidOperationException($"Only {selected.Count} safe relic locations were generated for {layout.challengeId}.");
            }

            for (var index = 0; index < selected.Count; index++)
            {
                layout.relics.Add(new RelicDefinition
                {
                    id = $"relic-{index + 1:D2}",
                    position = selected[index],
                    styleIndex = random.Range(0, 4),
                    bonus = index == selected.Count - 1
                });
            }
        }

        private static bool IsInsideBuilding(Vector3 point, IReadOnlyList<BuildingDefinition> buildings)
        {
            foreach (var building in buildings)
            {
                var half = building.size * 0.5f + new Vector3(2.2f, 0f, 2.2f);
                if (Mathf.Abs(point.x - building.position.x) < half.x &&
                    Mathf.Abs(point.z - building.position.z) < half.z)
                {
                    return true;
                }
            }

            return false;
        }

        private static void Shuffle<T>(IList<T> items, DeterministicRandom random)
        {
            for (var index = items.Count - 1; index > 0; index--)
            {
                var swap = random.Range(0, index + 1);
                (items[index], items[swap]) = (items[swap], items[index]);
            }
        }
    }
}
