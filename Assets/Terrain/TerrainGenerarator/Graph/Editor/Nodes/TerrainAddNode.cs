using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

[Serializable]
internal class TerrainAddNode : TerrainNode
{
    public const string INPUT_A_PORT = "HeigtmapA In";
    public const string INPUT_B_PORT = "HeigtmapB In";
    public const string OutputPortName = "Heigtmap Out";

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort<RenderTexture>(INPUT_A_PORT);
        context.AddInputPort<RenderTexture>(INPUT_B_PORT);

        context.AddOutputPort<RenderTexture>(OutputPortName);
    }
}