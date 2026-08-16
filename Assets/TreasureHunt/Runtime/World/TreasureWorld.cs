using System;
using System.Collections.Generic;
using SignalHunt.Core;
using UnityEngine;

namespace TreasureHunt.World
{
    public enum TreasurePropKind
    {
        Ruin,
        Tree,
        Rock,
        Crystal,
        Pillar
    }

    [Serializable]
    public sealed class TreasureWorldLayout
    {
        public string challengeId;
        public Vector3 playerSpawn;
        public float playerHeading;
        public List<TreasureArtifactDefinition> artifacts = new();
        public List<TreasurePropDefinition> props = new();
        public List<TreasurePathDefinition> paths = new();
        public List<TreasureGroundPatch> patches = new();
    }

    [Serializable]
    public struct TreasureArtifactDefinition
    {
        public string id;
        public Vector3 position;
        public int styleIndex;
    }

    [Serializable]
    public struct TreasurePropDefinition
    {
        public TreasurePropKind kind;
        public Vector3 position;
        public Vector3 scale;
        public float heading;
        public int styleIndex;
    }

    [Serializable]
    public struct TreasurePathDefinition
    {
        public Vector3 start;
        public Vector3 end;
        public float width;
    }

    [Serializable]
    public struct TreasureGroundPatch
    {
        public Vector3 position;
        public Vector3 scale;
        public int styleIndex;
    }

    public static class TreasureWorldGenerator
    {
        public static TreasureWorldLayout Generate(DailyChallenge challenge)
        {
            if (challenge == null || challenge.appKey != DailyGameCatalog.TreasureHuntAppKey)
            {
                throw new ArgumentException("A Treasure Hunter challenge is required.", nameof(challenge));
            }
            var random = new DeterministicRandom(challenge.generationSeed);
            var layout = new TreasureWorldLayout
            {
                challengeId = challenge.challengeId,
                playerSpawn = new Vector3(0f, 1.1f, -78f),
                playerHeading = 0f
            };
            GenerateArtifacts(layout, random);
            if (challenge.stageKey == "sunken-ruins")
            {
                GenerateRuins(layout, random);
            }
            else
            {
                GenerateCrystalHollow(layout, random);
            }
            return layout;
        }

        private static void GenerateArtifacts(TreasureWorldLayout layout, DeterministicRandom random)
        {
            for (var index = 0; index < 12; index++)
            {
                Vector3 candidate = default;
                var accepted = false;
                for (var attempt = 0; attempt < 96 && !accepted; attempt++)
                {
                    candidate = new Vector3(random.Range(-66f, 66f), 0.8f, random.Range(-62f, 67f));
                    accepted = Vector3.Distance(candidate, layout.playerSpawn) > 17f &&
                               IsSeparated(candidate, layout.artifacts, 16f);
                }
                if (!accepted)
                {
                    var angle = index * Mathf.PI * 2f / 12f;
                    candidate = new Vector3(Mathf.Cos(angle) * 58f, 0.8f, Mathf.Sin(angle) * 58f);
                }
                layout.artifacts.Add(new TreasureArtifactDefinition
                {
                    id = $"artifact-{index + 1:D2}",
                    position = candidate,
                    styleIndex = index % 4
                });
            }
        }

        private static bool IsSeparated(Vector3 candidate, IReadOnlyList<TreasureArtifactDefinition> artifacts,
            float minimum)
        {
            for (var index = 0; index < artifacts.Count; index++)
            {
                if (Vector3.Distance(candidate, artifacts[index].position) < minimum)
                {
                    return false;
                }
            }
            return true;
        }

        private static void GenerateRuins(TreasureWorldLayout layout, DeterministicRandom random)
        {
            for (var index = 0; index < 8; index++)
            {
                var angle = index * Mathf.PI * 2f / 8f;
                layout.paths.Add(new TreasurePathDefinition
                {
                    start = Vector3.zero,
                    end = new Vector3(Mathf.Cos(angle) * 82f, 0f, Mathf.Sin(angle) * 82f),
                    width = random.Range(4.5f, 7f)
                });
            }
            AddArtifactCover(layout, random, TreasurePropKind.Ruin);
            AddProps(layout, random, TreasurePropKind.Ruin, 28, 7f, 15f, 1.5f, 6f);
            AddProps(layout, random, TreasurePropKind.Tree, 44, 1.4f, 3f, 3.5f, 8f);
            for (var index = 0; index < 22; index++)
            {
                layout.patches.Add(new TreasureGroundPatch
                {
                    position = new Vector3(random.Range(-82f, 82f), 0f, random.Range(-82f, 82f)),
                    scale = new Vector3(random.Range(5f, 15f), 0.08f, random.Range(5f, 15f)),
                    styleIndex = random.Range(0, 4)
                });
            }
        }

        private static void GenerateCrystalHollow(TreasureWorldLayout layout, DeterministicRandom random)
        {
            for (var index = 0; index < 5; index++)
            {
                var angle = index * Mathf.PI * 2f / 5f + 0.35f;
                layout.paths.Add(new TreasurePathDefinition
                {
                    start = Vector3.zero,
                    end = new Vector3(Mathf.Cos(angle) * 80f, 0f, Mathf.Sin(angle) * 80f),
                    width = random.Range(3.5f, 5.5f)
                });
            }
            AddArtifactCover(layout, random, TreasurePropKind.Crystal);
            AddProps(layout, random, TreasurePropKind.Rock, 42, 2f, 7f, 1.5f, 6f);
            AddProps(layout, random, TreasurePropKind.Crystal, 38, 0.8f, 2.8f, 2f, 8f);
            AddProps(layout, random, TreasurePropKind.Pillar, 18, 1.8f, 4f, 4f, 11f);
            for (var index = 0; index < 22; index++)
            {
                layout.patches.Add(new TreasureGroundPatch
                {
                    position = new Vector3(random.Range(-82f, 82f), 0f, random.Range(-82f, 82f)),
                    scale = new Vector3(random.Range(4f, 13f), 0.08f, random.Range(4f, 13f)),
                    styleIndex = random.Range(0, 4)
                });
            }
        }

        private static void AddArtifactCover(TreasureWorldLayout layout, DeterministicRandom random,
            TreasurePropKind kind)
        {
            foreach (var artifact in layout.artifacts)
            {
                var angle = random.Range(0f, Mathf.PI * 2f);
                var offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * random.Range(3.2f, 4.6f);
                layout.props.Add(new TreasurePropDefinition
                {
                    kind = kind,
                    position = artifact.position + offset - Vector3.up * artifact.position.y,
                    scale = kind == TreasurePropKind.Ruin
                        ? new Vector3(random.Range(3f, 6f), random.Range(2.5f, 5f), random.Range(2f, 4f))
                        : new Vector3(random.Range(0.9f, 1.8f), random.Range(3f, 6f), random.Range(0.9f, 1.8f)),
                    heading = random.Range(0f, 360f),
                    styleIndex = artifact.styleIndex
                });
            }
        }

        private static void AddProps(TreasureWorldLayout layout, DeterministicRandom random, TreasurePropKind kind,
            int count, float minimumWidth, float maximumWidth, float minimumHeight, float maximumHeight)
        {
            for (var index = 0; index < count; index++)
            {
                var position = new Vector3(random.Range(-84f, 84f), 0f, random.Range(-84f, 84f));
                layout.props.Add(new TreasurePropDefinition
                {
                    kind = kind,
                    position = position,
                    scale = new Vector3(random.Range(minimumWidth, maximumWidth),
                        random.Range(minimumHeight, maximumHeight), random.Range(minimumWidth, maximumWidth)),
                    heading = random.Range(0f, 360f),
                    styleIndex = random.Range(0, 4)
                });
            }
        }
    }
}
