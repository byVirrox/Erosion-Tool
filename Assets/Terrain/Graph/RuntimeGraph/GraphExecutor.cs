using System.Collections.Generic;
using UnityEngine;

public class GraphExecutor : IGraphGenerator
{
    private readonly Dictionary<System.Type, ITerrainNodeExecutor> m_Executors;

    public GraphExecutor(GraphExecutorConfig config)
    {
        m_Executors = new Dictionary<System.Type, ITerrainNodeExecutor>
        {
            { typeof(FbmRuntimeNode), new FbmNodeExecutor(config.FbmShader, config.PermutationBuffer) },
            
            { typeof(AddRuntimeNode), new AddNodeExecutor(config.AddShader) },

            { typeof(OperationRuntimeNode), new OperationNodeExecutor(config.OperationShader) },

            { typeof(SplineRuntimeNode), new SplineNodeExecutor(config.SplineShader) }
        };
    }

    public RenderTexture Execute(TerrainRuntimeGraph graph, TerrainGenerationContext context)
    {
        if (graph == null || graph == null)
        {
            Debug.LogError("Execute called with invalid or null context/graph.");
            return null;
        }

        var executionContext = new GraphExecutorContext
        {
            Coords = context.Coords,
            Resolution = context.Resolution,
            BorderSize = context.BorderSize,
        };

        RenderTexture finalResult = null;

        try
        {
            OutputRuntimeNode outputNode = null;


            foreach (var node in graph.nodes)
            {
                if (node is OutputRuntimeNode output)
                {
                    outputNode = output;
                    continue;
                }

                if (m_Executors.TryGetValue(node.GetType(), out var executor))
                {
                    executor.Execute(node, executionContext);
                }
            }

            if (outputNode != null && executionContext.NodeResults.TryGetValue(outputNode.finalInputNodeId, out var result))
            {
                finalResult = result;
            }

            return finalResult;
        }
        finally
        {
            foreach (var kvp in executionContext.NodeResults)
            {
                var texture = kvp.Value;
                if (texture != null && texture != finalResult)
                {
                    RenderTexture.ReleaseTemporary(texture);
                }
            }
        }
    }

}