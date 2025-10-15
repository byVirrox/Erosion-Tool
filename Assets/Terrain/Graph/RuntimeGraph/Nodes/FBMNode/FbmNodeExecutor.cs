using UnityEngine;

public class FbmNodeExecutor : ITerrainNodeExecutor
{
    private readonly ComputeShader m_FbmShader;
    private readonly ComputeBuffer m_PermutationBuffer;
    private readonly int m_Kernel;

    public FbmNodeExecutor(ComputeShader fbmShader, ComputeBuffer permutationBuffer)
    {
        m_FbmShader = fbmShader;
        m_PermutationBuffer = permutationBuffer;
        m_Kernel = m_FbmShader.FindKernel("GenerateFbmNoise");
    }

    public void Execute(TerrainRuntimeNode node, GraphExecutorContext context)
    {
        var fbmNode = node as FbmRuntimeNode;
        if (fbmNode == null) 
            return;

        RenderTexture resultTexture = RunFbmShader(fbmNode, context);

        context.NodeResults[fbmNode.nodeId] = resultTexture;
    }

    /// <summary>
    /// Führt den FBM Compute Shader mit den gegebenen Parametern aus.
    /// Diese Methode ist im Grunde eine Kopie deiner alten GenerateTexture-Methode.
    /// </summary>
    private RenderTexture RunFbmShader(FbmRuntimeNode fbmNode, GraphExecutorContext context)
    {
        if (m_FbmShader == null)
        {
            Debug.LogError("FBM Shader ist nicht zugewiesen.");
            return null;
        }

        int kernel = m_FbmShader.FindKernel("GenerateFbmNoise");
        int targetResolution = context.Resolution + context.BorderSize * 2;

        RenderTexture targetTexture = RenderTexture.GetTemporary(targetResolution, targetResolution, 0, RenderTextureFormat.RFloat);
        targetTexture.enableRandomWrite = true;

        Vector2 finalScale = new Vector2(fbmNode.scale * fbmNode.xScale, fbmNode.scale * fbmNode.yScale);
        float finalHeightScale = fbmNode.scale * fbmNode.heightScale;

        m_FbmShader.SetInts("chunkCoord", new int[] { context.Coords.X, context.Coords.Y });
        m_FbmShader.SetInt("resolution", context.Resolution);
        m_FbmShader.SetInt("borderSize", context.BorderSize);

        m_FbmShader.SetVector("offset", fbmNode.offset);
        m_FbmShader.SetVector("scale", finalScale);
        m_FbmShader.SetFloat("persistence", fbmNode.persistence);
        m_FbmShader.SetFloat("lacunarity", fbmNode.lacunarity);
        m_FbmShader.SetFloat("heightScale", finalHeightScale);
        m_FbmShader.SetInt("octaves", fbmNode.octaves);
        m_FbmShader.SetInt("ridgedOctaves", fbmNode.ridgedOctaves);
        m_FbmShader.SetInt("noiseType", (int)fbmNode.noiseType);
        m_FbmShader.SetInt("fbmType", (int)fbmNode.fbmType);

        if (m_PermutationBuffer != null)
        {
            m_FbmShader.SetBuffer(kernel, "perm", m_PermutationBuffer);
        }
        m_FbmShader.SetTexture(kernel, "Result", targetTexture);

        int threadGroups = Mathf.CeilToInt(targetResolution / 8.0f);
        m_FbmShader.Dispatch(kernel, threadGroups, threadGroups, 1);

        return targetTexture;
    }
}
