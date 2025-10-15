using UnityEngine;

public class OperationNodeExecutor : ITerrainNodeExecutor
{
    private readonly ComputeShader m_OperationShader;

    public OperationNodeExecutor(ComputeShader operationShader)
    {
        m_OperationShader = operationShader;
    }

    public void Execute(TerrainRuntimeNode node, GraphExecutorContext context)
    {
        var opNode = node as OperationRuntimeNode;
        if (opNode == null || m_OperationShader == null) return;

        context.NodeResults.TryGetValue(opNode.inputNodeIdA, out RenderTexture texA);
        context.NodeResults.TryGetValue(opNode.inputNodeIdB, out RenderTexture texB);

        RenderTexture resultTexture = null;
        string kernelName = opNode.opType.ToString();
        int kernel = m_OperationShader.FindKernel(kernelName);

        if (kernel == -1)
        {
            Debug.LogError($"Kernel '{kernelName}' not found in Operation Shader.");
            context.NodeResults[opNode.nodeId] = CreateEmpty(context.Resolution);
            return;
        }

        switch (opNode.opType)
        {
            case OperationType.Add:
            case OperationType.Subtract:
            case OperationType.Multiply:
            case OperationType.Divide:
                if (texA != null && texB != null)
                {
                    resultTexture = RunTextureTextureOp(kernel, texA, texB);
                }
                else if (texA != null) { resultTexture = CreateCopy(texA); }
                break;

            case OperationType.AddByValue:
            case OperationType.SubtractByValue:
            case OperationType.MultiplyByValue:
            case OperationType.DivideByValue:
            case OperationType.Power:
                if (texA != null)
                {
                    resultTexture = RunTextureValueOp(kernel, texA, opNode.value);
                }
                break;
        }

        if (resultTexture == null)
        {
            resultTexture = CreateEmpty(context.Resolution + context.BorderSize * 2);
        }

        context.NodeResults[opNode.nodeId] = resultTexture;
    }

    private RenderTexture RunTextureTextureOp(int kernel, RenderTexture inputA, RenderTexture inputB)
    {
        RenderTexture result = RenderTexture.GetTemporary(inputA.descriptor);
        result.enableRandomWrite = true;

        m_OperationShader.SetTexture(kernel, "InputA", inputA);
        m_OperationShader.SetTexture(kernel, "InputB", inputB);
        m_OperationShader.SetTexture(kernel, "Result", result);

        DispatchShader(kernel, result);
        return result;
    }

    private RenderTexture RunTextureValueOp(int kernel, RenderTexture inputA, float value)
    {
        RenderTexture result = RenderTexture.GetTemporary(inputA.descriptor);
        result.enableRandomWrite = true;

        m_OperationShader.SetTexture(kernel, "InputA", inputA);
        m_OperationShader.SetFloat("Value", value);
        m_OperationShader.SetTexture(kernel, "Result", result);

        DispatchShader(kernel, result);
        return result;
    }


    private void DispatchShader(int kernel, RenderTexture target)
    {
        int threadGroupsX = Mathf.CeilToInt(target.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(target.height / 8.0f);

        m_OperationShader.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);
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
