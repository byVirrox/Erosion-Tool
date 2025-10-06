using Unity.GraphToolkit.Editor;
using UnityEngine;

[System.Serializable]
public class AddNode : Node, IHeightmapEvaluatorNode
{
    // Eindeutige Namen für die Ports
    public const string InputAPortName = "A";
    public const string InputBPortName = "B";
    public const string OutputPortName = "Out";

    // Definiere die Ein- und Ausgänge des Knotens
    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        // Typisierte Ports für maximale Sicherheit!
        context.AddInputPort<RenderTexture>(InputAPortName);
        context.AddInputPort<RenderTexture>(InputBPortName);
        context.AddOutputPort<RenderTexture>(OutputPortName);
    }

    // Die Implementierung der Berechnungslogik
    public RenderTexture Evaluate(TerrainGraphProcessor graphProcessor, IChunk chunk)
    {
        return null;
        /**
        var portA = GetInputPort(InputAPortName);
        var portB = GetInputPort(InputBPortName);

        IHeightmapEvaluatorNode inputNodeA = portA.Connection?.output.node as IHeightmapEvaluatorNode;
        IHeightmapEvaluatorNode inputNodeB = portB.Connection?.output.node as IHeightmapEvaluatorNode;

        if (inputNodeA == null || inputNodeB == null)
        {
            Debug.LogError("AddNode benötigt zwei verbundene Eingänge.");
            return null;
        }

        RenderTexture textureA = graphProcessor.ResolvePortValue(inputNodeA, chunk);
        RenderTexture textureB = graphProcessor.ResolvePortValue(inputNodeB, chunk);


        RenderTexture resultTexture = new RenderTexture(textureA.descriptor);

        return resultTexture;
        **/
    }
}