using UnityEngine;

/// <summary>
/// Contains all dependencies required to execute a terrain graph.
/// </summary>
public class GraphExecutorConfig
{
    public ComputeShader FbmShader { get; set; }
    public ComputeShader OperationShader { get; set; }
    public ComputeShader SplineShader { get; set; }
    public ComputeBuffer PermutationBuffer { get; set; }
}