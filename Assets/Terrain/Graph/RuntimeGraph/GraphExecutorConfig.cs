using UnityEngine;

/// <summary>
/// Enthält alle Abhängigkeiten, die für die Ausführung eines Terrain-Graphen benötigt werden.
/// </summary>
public class GraphExecutorConfig
{
    public ComputeShader FbmShader { get; set; }
    public ComputeShader AddShader { get; set; }
    public ComputeShader OperationShader { get; set; }
    public ComputeShader SplineShader { get; set; }
    public ComputeBuffer PermutationBuffer { get; set; }
}