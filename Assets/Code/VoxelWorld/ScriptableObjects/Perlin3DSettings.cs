using System;
using UnityEngine;

namespace VoxelWorld
{
    [CreateAssetMenu(fileName = "Perlin3DConfiguration",menuName = "VoxelWorld/Perlin3D Configuration")]
    public class Perlin3DSettings : ScriptableObject
    {
        [Range(1, 10)]
        public float heightScale = 1;

        [Range(0.01f, 1)]
        public float scale = 1f;

        [Range(1, 10)]
        public int octaves = 1;

        [Range(-20, 20)]
        public float heightOffset = 0;

        [Range(1, 10)]
        public float drawCutOff = 1;

        public Settings ToValueType()
        {
            return new Settings()
            {
                heightScale = heightScale,
                scale = scale,
                octaves = octaves,
                heightOffset = heightOffset,
                drawCutOff = drawCutOff
            };
        }

        public struct Settings
        {
            public float heightScale;
            public float scale;
            public int octaves;
            public float heightOffset;
            public float drawCutOff;
        }
    }
}

