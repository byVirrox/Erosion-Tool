using UnityEngine;

public class SplineNodeExecutor : ITerrainNodeExecutor
{
    private readonly ComputeShader m_SplineShader;

    public SplineNodeExecutor(ComputeShader splineShader)
    {
        m_SplineShader = splineShader;
    }

    public void Execute(TerrainRuntimeNode node, GraphExecutorContext context)
    {
        var splineNode = node as SplineRuntimeNode;
        if (splineNode == null || m_SplineShader == null)
        {
            return;
        }

        if (!context.NodeResults.TryGetValue(splineNode.inputNodeId, out RenderTexture inputTexture) || inputTexture == null)
        {
            context.NodeResults[splineNode.nodeId] = CreateEmpty(context.Resolution);
            return;
        }

        if (splineNode.bakedCurveSamples == null || splineNode.bakedCurveSamples.Length < 2)
        {
            context.NodeResults[splineNode.nodeId] = CreateCopy(inputTexture);
            return;
        }

        ComputeBuffer curveBuffer = new ComputeBuffer(splineNode.bakedCurveSamples.Length, sizeof(float));
        RenderTexture resultTexture = RenderTexture.GetTemporary(inputTexture.descriptor);
        resultTexture.enableRandomWrite = true;

        try
        {
            curveBuffer.SetData(splineNode.bakedCurveSamples);

            int kernel = m_SplineShader.FindKernel("RemapValues");

            m_SplineShader.SetTexture(kernel, "InputTexture", inputTexture);
            m_SplineShader.SetTexture(kernel, "Result", resultTexture);
            m_SplineShader.SetBuffer(kernel, "BakedCurve", curveBuffer);
            m_SplineShader.SetInt("SampleCount", splineNode.bakedCurveSamples.Length);
            m_SplineShader.SetFloat("InputMin", splineNode.inputMin);
            m_SplineShader.SetFloat("InputMax", splineNode.inputMax);

            int threadGroupsX = Mathf.CeilToInt(resultTexture.width / 8.0f);
            int threadGroupsY = Mathf.CeilToInt(resultTexture.height / 8.0f);
            m_SplineShader.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);

            context.NodeResults[splineNode.nodeId] = resultTexture;
        }
        finally
        {
            curveBuffer.Release();
        }
    }


    private RenderTexture CreateCopy(RenderTexture source)
    {
        if (source == null) return null;
        RenderTexture copy = RenderTexture.GetTemporary(source.descriptor);
        Graphics.Blit(source, copy);
        return copy;
    }


    private RenderTexture CreateEmpty(int resolution)
    {
        RenderTexture empty = RenderTexture.GetTemporary(resolution, resolution, 0, RenderTextureFormat.RFloat);
        return empty;
    }
}
