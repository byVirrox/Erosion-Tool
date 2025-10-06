using UnityEngine;
using static TreeEditor.TreeEditorHelper;

public enum NoiseTypeEnum
{
    Perlin,
    Simplex,
    Value,
}

public class NoiseFunction : INoiseFunction
{
    private readonly NoiseTypeEnum noiseType;
    private readonly int seed;
    private readonly float scale;

    public NoiseFunction(NoiseTypeEnum type, int seed, float scale)
    {
        this.noiseType = type;
        this.seed = seed;
        this.scale = scale;
    }

    public float GetValue(float x, float y)
    {
        float noiseX = (x + seed) / scale;
        float noiseY = (y + seed) / scale;

        switch (noiseType)
        {
            case NoiseTypeEnum.Perlin:
                return Mathf.PerlinNoise(noiseX, noiseY);
            case NoiseTypeEnum.Simplex:
            // You'd use a third-party library or your own implementation here
            // return SimplexNoise.GetValue(noiseX, noiseY);
            case NoiseTypeEnum.Value:
            // return your custom Value Noise implementation
            default:
                return 0; // Return a default value for safety
        }
    }
}
