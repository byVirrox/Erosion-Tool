using System.Collections.Generic;
using Unity.GraphToolkit.Editor;
using UnityEngine;

public class TerrainGraphProcessor
{
    private Dictionary<INode, RenderTexture> _cachedResults = new Dictionary<INode, RenderTexture>();

    public RenderTexture EvaluateGraph(IHeightmapEvaluatorNode finalNode, IChunk chunk)
    {
        _cachedResults.Clear();
        return ResolvePortValue(finalNode, chunk);
    }

    public RenderTexture ResolvePortValue(IHeightmapEvaluatorNode nodeToEvaluate, IChunk chunk)
    {
        if (_cachedResults.TryGetValue(nodeToEvaluate, out var cachedTexture))
        {
            return cachedTexture;
        }

        RenderTexture result = nodeToEvaluate.Evaluate(this, chunk);

        _cachedResults[nodeToEvaluate] = result;
        return result;
    }
}
