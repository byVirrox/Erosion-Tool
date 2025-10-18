using UnityEngine;

public interface IGraphGenerator
{
    /// <summary>
    /// Executes a terrain generation graph with the given context.
    /// </summary>
    /// <param name="graph">The runtime graph asset to execute.</param>
    /// <param name="coords">The grid coordinates for the chunk.</param>
    /// <param name="resolution">The base resolution of the texture.</param>
    /// <param name="borderSize">The border size to add.</param>
    /// <returns>The generated RenderTexture.</returns>
    RenderTexture Execute(TerrainRuntimeGraph graph, TerrainGenerationContext context);
}
