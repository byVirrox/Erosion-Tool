using UnityEngine;

[CreateAssetMenu(fileName = "NewFbmConfig", menuName = "Terrain/FBM Configuration")]
public class FbmConfig : ScriptableObject
{
    [Header("FBM Noise Settings")]
    public Vector2 offset = new Vector2(0f, 0f);

    [Min(0)]
    public float scale = 1f;

    [Range(0.0001f, 0.1f)]
    public float xScale = 0.01f;

    [Range(0.0001f, 0.1f)]
    public float yScale = 0.01f;

    public float heightScale = 3f; 
    public float persistence = 0.5f; 
    public float lacunarity = 2.0f;

    [Min(1)]
    public int octaves = 8;

    public NoiseType noiseType = NoiseType.Perlin;
    public FBMType fbmType = FBMType.StandardFBM;
}
