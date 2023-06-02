namespace VoxelWorld
{
    using System;

    public struct PerlinSettings
    {
        public float heightScale;
        public float scale;
        public int octaves;
        public float heightOffset;
        public float probability;

        public PerlinSettings(float heightScale, float scale, int octaves, float heightOffset, float probability)
        {
            this.heightScale = heightScale;
            this.scale = scale;
            this.octaves = octaves;
            this.heightOffset = heightOffset;
            this.probability = probability;
        }
    }
}