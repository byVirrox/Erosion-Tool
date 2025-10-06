using Unity.GraphToolkit.Editor;
using UnityEngine;

public struct HeightmapData { }


public abstract class TerrainNodeBase : Node
{
    public const string EXECUTION_PORT_DEFAULT_NAME = "Terrain Node";


    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort<HeightmapData>("Input").Build();
        context.AddOutputPort<HeightmapData>("Output").Build();
    }
}