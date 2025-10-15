using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

/// <summary>
/// Der finale Output-Knoten des Terrain-Graphen.
/// Er akzeptiert die finale RenderTexture, die als Heightmap für einen Chunk dient.
/// </summary>
[Serializable]
internal class TerrainOutputNode : TerrainNode
{
    public const string INPUT_PORT_NAME = "Heightmap In";

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort<RenderTexture>(INPUT_PORT_NAME).Build();
    }
}