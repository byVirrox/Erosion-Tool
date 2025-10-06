using System;
using System.Linq;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;


[Graph(AssetExtension)]
[Serializable]
public class TerrainGraph : Graph
{
    public int outputResolution = 1025;

    public const string AssetExtension = "simpleg";


    public static void CreateAsset()
    {
        GraphDatabase.PromptInProjectBrowserToCreateNewAsset<TerrainGraph>();
    }

    private void CheckGraphErrors(GraphLogger infos)
    {
        var terrainOutputNodes = GetNodes().OfType<TerrainOutputNode>().ToList();
        switch (terrainOutputNodes.Count)
        {
            case 0:
                infos.LogError("Add a CreateTextureNode in your Texture graph.");
                break;
            case > 1:
                {
                    foreach (var terrainOutputNode in terrainOutputNodes.Skip(1))
                    {
                        infos.LogWarning($"TerrainGraph only supports one {nameof(TerrainOutputNode)} by graph. " +
                                         "Only the first created one will be used.", terrainOutputNode);
                    }
                    break;
                }
        }
    }

    public override void OnGraphChanged(GraphLogger infos)
    {
        CheckGraphErrors(infos);
    }

    public static T ResolvePortValue<T>(IPort port)
    {
        /**
        var sourcePort = port.firstConnectedPort;

        switch (sourcePort?.GetNode())
        {
            case IConstantNode node:
                node.TryGetValue(out T constantValue);
                return constantValue;
            case IVariableNode node:
                node.variable.TryGetDefaultValue(out T variableValue);
                return variableValue;
            case ITextureEvaluatorNode textureEvaluatorNode:
                if (typeof(T).IsAssignableFrom(typeof(Texture2D)))
                {
                    return (T)(object)textureEvaluatorNode.EvaluateTexturePort(sourcePort);
                }
                break;
            case null:
                // If no connection exists, try to get "port" 's embedded value (returns type default if unavailable)
                port.TryGetValue(out T embeddedValue);
                return embeddedValue;
        }
        **/
        return default;
    }

}