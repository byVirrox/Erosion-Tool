using System;
using UnityEngine;

[Serializable]
internal class TerrainFBMNode : TerrainNodeBase
{
    public const string OFFSET_PORT = "Offset";
    public const string SCALE_PORT = "Scale";
    public const string XSCALE_PORT = "X Scale";
    public const string YSCALE_PORT = "Y Scale";
    public const string HEIGHT_SCALE_PORT = "Height Scale";
    public const string PERSISTENCE_PORT = "Persistence";
    public const string LACUNARITY_PORT = "Lacunarity";
    public const string OCTAVES_PORT = "Octaves";
    public const string RIDGED_OCTAVES_PORT = "Ridged Octaves";
    public const string NOISE_TYPE_PORT = "Noise Type";
    public const string FBM_TYPE_PORT = "FBM Type";
    public const string OUTPUT_PORT = "Heightmap Out";

    public enum NoiseType { Perlin, Simplex, Cellular }
    public enum FBMType { StandardFBM, RidgedFBM }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort<Vector2>(OFFSET_PORT).WithDefaultValue(Vector2.zero).Build();
        context.AddInputPort<float>(SCALE_PORT).WithDefaultValue(1.0f).Build();
        context.AddInputPort<float>(XSCALE_PORT).WithDefaultValue(0.001f).Build();
        context.AddInputPort<float>(YSCALE_PORT).WithDefaultValue(0.010f).Build();
        context.AddInputPort<float>(HEIGHT_SCALE_PORT).WithDefaultValue(0.3f).Build();
        context.AddInputPort<float>(PERSISTENCE_PORT).WithDefaultValue(0.5f).Build();
        context.AddInputPort<float>(LACUNARITY_PORT).WithDefaultValue(2.0f).Build();
        context.AddInputPort<int>(OCTAVES_PORT).WithDefaultValue(8).Build();
        context.AddInputPort<int>(RIDGED_OCTAVES_PORT).WithDefaultValue(0).Build();

        context.AddInputPort<NoiseType>(NOISE_TYPE_PORT).WithDefaultValue(NoiseType.Perlin).Build();
        context.AddInputPort<FBMType>(FBM_TYPE_PORT).WithDefaultValue(FBMType.StandardFBM).Build();

        context.AddOutputPort<RenderTexture>(OUTPUT_PORT).Build();
    }
}