using System;
using UnityEngine;

[Serializable]
public class FbmRuntimeNode : TerrainRuntimeNode
{
    public Vector2 offset;
    public float scale;
    public float xScale;
    public float yScale;
    public float heightScale;
    public float persistence;
    public float lacunarity;
    public int octaves;
    public int ridgedOctaves;
    public NoiseType noiseType;
    public FBMType fbmType;
}
