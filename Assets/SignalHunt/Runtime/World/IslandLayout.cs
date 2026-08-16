using System;
using System.Collections.Generic;
using UnityEngine;

namespace SignalHunt.World
{
    [Serializable]
    public sealed class IslandLayout
    {
        public string challengeId;
        public uint terrainSeed;
        public float worldSize;
        public float waterLevel;
        public int terrainResolution;
        public Vector3 playerSpawn;
        public float playerHeading;
        public List<IslandHillDefinition> hills = new();
        public List<Vector3> roadPoints = new();
        public List<IslandTreeDefinition> trees = new();
        public List<IslandRockDefinition> rocks = new();
        public List<IslandRampDefinition> ramps = new();
        public List<RelicDefinition> relics = new();
    }

    [Serializable]
    public struct IslandHillDefinition
    {
        public Vector2 center;
        public float radius;
        public float height;
    }

    [Serializable]
    public struct IslandTreeDefinition
    {
        public Vector3 position;
        public float scale;
        public int styleIndex;
    }

    [Serializable]
    public struct IslandRockDefinition
    {
        public Vector3 position;
        public Vector3 scale;
        public float yaw;
        public int styleIndex;
    }

    [Serializable]
    public struct IslandRampDefinition
    {
        public Vector3 position;
        public Vector3 scale;
        public float yaw;
        public int styleIndex;
    }
}
