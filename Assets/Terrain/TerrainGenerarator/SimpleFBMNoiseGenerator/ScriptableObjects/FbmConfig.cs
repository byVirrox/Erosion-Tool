using UnityEngine;

[CreateAssetMenu(fileName = "NewFbmConfig", menuName = "Terrain/FBM Configuration")]
public class FbmConfig : ScriptableObject
{
    [Header("General Transform")]
    [Tooltip("Offsets the noise sampling coordinates on the X and Y axes.")]
    public Vector2 offset = new Vector2(0f, 0f);

    [Tooltip("Overall master scale of the noise pattern. Multiplies with xScale and yScale.")]
    [Min(0)]
    public float scale = 1f;

    [Header("Noise Frequency & Amplitude")]
    [Tooltip("Controls the frequency (stretching) of the noise along the X-axis.")]
    [Range(0.0001f, 0.1f)]
    public float xScale = 0.01f;

    [Tooltip("Controls the frequency (stretching) of the noise along the Y-axis.")]
    [Range(0.0001f, 0.1f)]
    public float yScale = 0.01f;

    [Tooltip("Multiplier for the final height value, controlling the terrain's vertical amplitude.")]
    public float heightScale = 3f;

    [Header("Fractal Brownian Motion (FBM)")]
    [Tooltip("Controls how much each successive octave contributes to the final shape. Lower values create smoother terrain.")]
    public float persistence = 0.5f;

    [Tooltip("Controls how much the frequency increases for each successive octave. Higher values create more detailed terrain.")]
    public float lacunarity = 2.0f;

    [Tooltip("The total number of noise layers to combine. More octaves add more detail at a higher performance cost.")]
    [Min(1)]
    public int octaves = 8;

    [Tooltip("Number of initial octaves to be inverted, creating valleys or ridges.")]
    [Min(0)]
    public int ridgedOctaves = 0;

    [Header("Algorithm Selection")]
    [Tooltip("The underlying noise algorithm to use as a base for the FBM.")]
    public NoiseType noiseType = NoiseType.Perlin;

    [Tooltip("The FBM combination algorithm to use. 'Standard' is a classic implementation. 'Advanced' uses domain warping for more complex features.")]
    public FBMType fbmType = FBMType.StandardFBM;


    private void OnValidate()
    {
        ridgedOctaves = Mathf.Clamp(ridgedOctaves, 0, octaves);
    }
}

