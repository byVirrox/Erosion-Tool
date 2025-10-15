using UnityEngine;

public class AddNodeExecutor : ITerrainNodeExecutor
{
    private readonly ComputeShader m_AddShader;
    private readonly int m_Kernel;

    public AddNodeExecutor(ComputeShader addShader)
    {
        m_AddShader = addShader;
        if (m_AddShader != null)
        {
            m_Kernel = m_AddShader.FindKernel("Add");
        }
    }

    public void Execute(TerrainRuntimeNode node, GraphExecutorContext context)
    {
        var addNode = node as AddRuntimeNode;
        if (addNode == null) return;

        context.NodeResults.TryGetValue(addNode.inputNodeIdA, out RenderTexture inputA);
        context.NodeResults.TryGetValue(addNode.inputNodeIdB, out RenderTexture inputB);

        RenderTexture resultTexture;

        if (inputA != null && inputB != null)
        {
            resultTexture = RunAddShader(inputA, inputB);
        }
        else if (inputA != null)
        {
            resultTexture = CreateCopy(inputA);
        }
        else if (inputB != null)
        {
            resultTexture = CreateCopy(inputB);
        }
        else
        {
            resultTexture = CreateEmpty(context.Resolution + context.BorderSize * 2);
        }

        context.NodeResults[addNode.nodeId] = resultTexture;
    }

    private RenderTexture RunAddShader(RenderTexture inputA, RenderTexture inputB)
    {
        RenderTexture result = RenderTexture.GetTemporary(inputA.width, inputA.height, 0, RenderTextureFormat.RFloat);
        result.enableRandomWrite = true;

        m_AddShader.SetTexture(m_Kernel, "InputA", inputA);
        m_AddShader.SetTexture(m_Kernel, "InputB", inputB);
        m_AddShader.SetTexture(m_Kernel, "Result", result);

        int threadGroupsX = Mathf.CeilToInt(result.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(result.height / 8.0f);
        m_AddShader.Dispatch(m_Kernel, threadGroupsX, threadGroupsY, 1);

        return result;
    }

    private RenderTexture CreateCopy(RenderTexture source)
    {
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