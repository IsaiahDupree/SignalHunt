using System;
using System.Collections.Generic;
using UnityEngine;

namespace SignalHunt.World
{
    [Serializable]
    public sealed class TownLayout
    {
        public string challengeId;
        public float worldSize;
        public Vector3 playerSpawn;
        public float playerHeading;
        public List<BuildingDefinition> buildings = new();
        public List<PropDefinition> props = new();
        public List<RelicDefinition> relics = new();
    }

    [Serializable]
    public struct BuildingDefinition
    {
        public Vector3 position;
        public Vector3 size;
        public int materialIndex;
        public bool isLandmark;
    }

    public enum TownPropKind
    {
        Ramp,
        SignalTower,
        LightPylon,
        TunnelGate
    }

    [Serializable]
    public struct PropDefinition
    {
        public TownPropKind kind;
        public Vector3 position;
        public Vector3 scale;
        public float yaw;
        public int materialIndex;
    }

    [Serializable]
    public struct RelicDefinition
    {
        public string id;
        public Vector3 position;
        public int styleIndex;
        public bool bonus;
    }
}
