using Unity.GraphToolkit.Editor;
using UnityEngine;

public interface IHeightmapEvaluatorNode : INode
{
    /// <summary>
    /// Wertet diesen Knoten und alle seine Abh�ngigkeiten aus, um die finale RenderTexture zu erzeugen.
    /// </summary>
    /// <param name="graphProcessor">Der Prozessor, der die Auswertung steuert.</param>
    /// <param name="chunk">Der Chunk, f�r den die Auswertung stattfindet.</param>
    /// <returns>Die berechnete RenderTexture.</returns>
    RenderTexture Evaluate(TerrainGraphProcessor graphProcessor, IChunk chunk);
}