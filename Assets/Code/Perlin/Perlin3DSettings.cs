using System;

namespace VoxelWorld
{

    public struct Perlin3DSettings
    {
        public float heightScale;
        public float scale;
        public int octaves;
        public float heightOffset;
        public float drawCutOff;

        public Perlin3DSettings(float heightScale, float scale, int octaves, float heightOffset, float drawCutOff)
        {
            this.heightScale = heightScale;
            this.scale = scale;
            this.octaves = octaves;
            this.heightOffset = heightOffset;
            this.drawCutOff = drawCutOff;
        }
    }
}