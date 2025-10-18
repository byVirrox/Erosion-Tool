/// <summary>
/// Defines a contract for a class that can execute the logic for a specific type of node
/// within a terrain generation graph. Each implementation is responsible for processing one
/// particular node type (e.g., FBM Noise, Operation, etc.).
public interface ITerrainNodeExecutor
{
    /// <summary>
    /// Executes the node's specific logic, processing inputs and storing the output
    /// within the provided execution context.
    /// </summary>
    /// <param name="node">The runtime node instance to be executed.</param>
    /// <param name="context">The shared context for the graph execution, containing chunk data and intermediate results from other nodes.</param>
    void Execute(TerrainRuntimeNode node, GraphExecutorContext context);
}
