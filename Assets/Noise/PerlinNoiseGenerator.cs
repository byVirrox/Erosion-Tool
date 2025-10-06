using UnityEngine;

public class PerlinNoiseGenerator : INoiseGenerator
{
    public int Seed { get; set; }
    public float Scale { get; set; }

    public PerlinNoiseGenerator(int seed, float scale)
    {
        Seed = seed;
        Scale = scale;
    }

    public float GetValue(float x, float z)
    {
        // Add the seed to the coordinates to get different patterns
        float noiseX = (x + Seed) / Scale;
        float noiseZ = (z + Seed) / Scale;

        // Mathf.PerlinNoise returns a value between 0.0 and 1.0
        return Mathf.PerlinNoise(noiseX, noiseZ);
    }
}
