using System;
using UnityEngine;

/// <summary>
/// The final output node of the terrain graph.
/// It accepts the final RenderTexture, which serves as a heightmap for a chunk.
/// </summary>
[Serializable]
internal class TerrainOutputNode : TerrainNodeBase
{
    public const string INPUT_PORT_NAME = "Heightmap In";

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort<RenderTexture>(INPUT_PORT_NAME).Build();
    }
}