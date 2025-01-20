using UnityEngine;
using System.Collections;

// Create a texture and fill it with Perlin noise.
// Try varying the xOrg, yOrg and scale values in the inspector
// while in Play mode to see the effect they have on the noise.

public class PerlinCreator : MonoBehaviour
{
    // Width and height of the texture in pixels.
    public int pixWidth;
    public int pixHeight;

    // The origin of the sampled area in the plane.
    public float xOrg;
    public float yOrg;

    // The number of cycles of the basic noise pattern that are repeated
    // over the width and height of the texture.
    public float scale = 1.0F;

    private Texture2D noiseTexture;
    private Color[] imageData;
    private new Renderer renderer;

    private void Start()
    {
        renderer = GetComponent<Renderer>();

        // Set up the texture and a Color array to hold pixels during processing.
        noiseTexture = new Texture2D(pixWidth, pixHeight);
        imageData = new Color[noiseTexture.width * noiseTexture.height];
        renderer.material.mainTexture = noiseTexture;
    }

    private void CalculateNoise()
    {
        // For each pixel in the texture...
        for (float y = 0.0F; y < noiseTexture.height; y++)
        {
            for (float x = 0.0F; x < noiseTexture.width; x++)
            {
                float xCoord = xOrg + x / noiseTexture.width * scale;
                float yCoord = yOrg + y / noiseTexture.height * scale;
                float sample = Mathf.PerlinNoise(xCoord, yCoord);
                imageData[(int)y * noiseTexture.width + (int)x] = new Color(sample, sample, sample);
            }
        }

        // Copy the pixel data to the texture and load it into the GPU.
        noiseTexture.SetPixels(imageData);
        noiseTexture.Apply();
    }

    private void Update()
    {
        CalculateNoise();
    }
}