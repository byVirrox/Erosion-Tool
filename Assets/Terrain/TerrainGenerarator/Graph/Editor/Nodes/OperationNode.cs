using System;
using UnityEngine;

[Serializable]
internal class OperationNode : TerrainNode
{
    public string OPERATION_NAME = "OperationType";

    public const string INPUT_A_TEX_PORT = "A";
    public const string INPUT_B_TEX_PORT = "B";
    public const string INPUT_VAL_PORT = "Value";
    public const string OUTPUT_PORT = "Out";

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption<OperationType>(OPERATION_NAME)
            .WithDisplayName("Operation")
            .WithDefaultValue(OperationType.Add)
            .Delayed();
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        var portOperationOption = GetNodeOptionByName(OPERATION_NAME);
        portOperationOption.TryGetValue<OperationType>(out var operation);

        switch (operation)
        {
            case OperationType.Add:
            case OperationType.Subtract:
            case OperationType.Multiply:
            case OperationType.Divide:
                context.AddInputPort<RenderTexture>(INPUT_A_TEX_PORT).Build();
                context.AddInputPort<RenderTexture>(INPUT_B_TEX_PORT).Build();
                break;

            case OperationType.AddByValue:
            case OperationType.SubtractByValue:
            case OperationType.MultiplyByValue:
            case OperationType.DivideByValue:
            case OperationType.Power:
                context.AddInputPort<RenderTexture>(INPUT_A_TEX_PORT).Build();
                context.AddInputPort<float>(INPUT_VAL_PORT).WithDefaultValue(1.0f).Build();
                break;
        }

        context.AddOutputPort<RenderTexture>(OUTPUT_PORT).Build();
    }
}