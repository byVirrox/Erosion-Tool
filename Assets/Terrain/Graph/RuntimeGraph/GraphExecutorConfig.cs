using UnityEngine;

/// <summary>
/// Enth�lt alle Abh�ngigkeiten, die f�r die Ausf�hrung eines Terrain-Graphen ben�tigt werden.
/// </summary>
public class GraphExecutorConfig
{
    public ComputeShader FbmShader { get; set; }
    public ComputeShader AddShader { get; set; }
    public ComputeShader OperationShader { get; set; }
    public ComputeShader SplineShader { get; set; }
    public ComputeBuffer PermutationBuffer { get; set; }
}