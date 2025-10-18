using System;
using UnityEngine;

[Serializable]
internal class SplineNode : TerrainNode
{
    private const string k_SampleRateOptionName = "Sample Rate";
    private const string k_SplineOptionName = "Remap Curve"; 

    public const string INPUT_PORT_NAME = "In";
    public const string OUTPUT_PORT_NAME = "Out";

    [SerializeField] public float inputMin = 0f;
    [SerializeField] public float inputMax = 1f;

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption<int>(k_SampleRateOptionName)
            .WithDisplayName(k_SampleRateOptionName)
            .WithDefaultValue(256)
            .Delayed();


        context.AddOption<AnimationCurve>(k_SplineOptionName)
            .WithDisplayName(k_SplineOptionName)
            .WithDefaultValue(AnimationCurve.Linear(0, 0, 1, 1))
            .Delayed();
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort<RenderTexture>(INPUT_PORT_NAME).Build();
        context.AddOutputPort<RenderTexture>(OUTPUT_PORT_NAME).Build();
    }

    public AnimationCurve GetSpline()
    {
        var splineField = GetNodeOptionByName(k_SplineOptionName);
        splineField.TryGetValue<AnimationCurve>(out var spline);
        return spline;
    }

    public int GetSampleRate()
    {
        var option = GetNodeOptionByName(k_SampleRateOptionName);
        option.TryGetValue<int>(out var rate);
        return rate > 1 ? rate : 2;
    }
}