using System;
using UnityEngine;

namespace VoxelWorld
{
    [CreateAssetMenu(fileName = "PerlinConfiguration", menuName = "VoxelWorld/Perlin Configuration")]
    public class PerlinSettings : ScriptableObject
    {
        [Range(1, 10)]
        public float heightScale;
        [Range(0f, 1f)]
        public float scale;
        [Range(1, 10)]
        public int octaves;
        [Range(-20, 20)]
        public float heightOffset;
        [Range(0, 1)]
        public float probability;

        public Settings ToValueType()
        {
            return new Settings()
            {
                heightScale = heightScale,
                scale = scale,
                octaves = octaves,
                heightOffset = heightOffset,
                probability = probability
            };
        }

        public struct Settings
        {
            public float heightScale;
            public float scale;
            public int octaves;
            public float heightOffset;
            public float probability;
        }
    }
}
