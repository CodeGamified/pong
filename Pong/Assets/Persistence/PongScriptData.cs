// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using CodeGamified.Persistence;
using UnityEngine;

namespace Pong.Persistence
{
    /// <summary>
    /// Serializable entity for a saved Pong paddle script.
    /// Stored in .data/scripts/ via the Persistence framework.
    /// </summary>
    [System.Serializable]
    public class PongScriptData
    {
        public string name;
        public string source;
        public int tier;
        public float bestScore;
    }

    /// <summary>
    /// JSON serializer for PongScriptData.
    /// </summary>
    public class PongScriptSerializer : IEntitySerializer<PongScriptData>
    {
        public int SchemaVersion => 1;

        public string Serialize(PongScriptData entity)
        {
            return JsonUtility.ToJson(entity, true);
        }

        public PongScriptData Deserialize(string json)
        {
            return JsonUtility.FromJson<PongScriptData>(json);
        }
    }
}
