using System;

[Serializable]
public class SplineRuntimeNode : TerrainRuntimeNode
{
    public int inputNodeId;
    public float inputMin;
    public float inputMax;

    public float[] bakedCurveSamples;
}