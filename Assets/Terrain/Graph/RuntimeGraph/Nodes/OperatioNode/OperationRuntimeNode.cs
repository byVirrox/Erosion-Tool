using System;

[Serializable]
public class OperationRuntimeNode : TerrainRuntimeNode
{
    public OperationType opType;

    public int inputNodeIdA;

    public int inputNodeIdB;

    public float value;
}