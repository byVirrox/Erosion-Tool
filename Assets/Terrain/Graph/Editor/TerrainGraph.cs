using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.Editor;
using UnityEditor;

[Graph(AssetExtension)]
[Serializable]
public class TerrainGraph : Graph
{
    public const string AssetExtension = "terraingraph";

    [MenuItem("Assets/Create/Terrain Graph")]
    public static void CreateAsset()
    {
        GraphDatabase.PromptInProjectBrowserToCreateNewAsset<TerrainGraph>();
    }

    public override void OnGraphChanged(GraphLogger infos)
    {
        CheckGraphErrors(infos);
    }

    private void CheckGraphErrors(GraphLogger infos)
    {
        var terrainOutputNodes = GetNodes().OfType<TerrainOutputNode>().ToList();

        switch (terrainOutputNodes.Count)
        {
            case 0:
                infos.LogError("The Graph should only have at least one TerrainOutputNode.");
                return; 
            case > 1:
                {
                    foreach (var terrainOutputNode in terrainOutputNodes.Skip(1))
                    {
                        infos.LogWarning($"Only one {nameof(TerrainOutputNode)} per Graph is supported. Only the first one will be used.", terrainOutputNode);
                    }
                    break;
                }
        }

        var outputNode = terrainOutputNodes.First();
        var visiting = new HashSet<INode>(); 
        var visited = new HashSet<INode>();

        HasCycle(outputNode, visiting, visited, infos);
    }

    /// <summary>
    /// Prüft rekursiv, ob vom gegebenen Knoten aus ein Zyklus erreichbar ist.
    /// </summary>
    /// <returns>True, wenn ein Zyklus gefunden wurde, ansonsten false.</returns>
    private bool HasCycle(INode node, HashSet<INode> visiting, HashSet<INode> visited, GraphLogger infos)
    {
        if (node == null) return false;

        visiting.Add(node);

        foreach (var inputPort in node.GetInputPorts())
        {
            if (!inputPort.isConnected || inputPort.firstConnectedPort == null) continue;

            var dependencyNode = inputPort.firstConnectedPort.GetNode();
            if (dependencyNode == null) continue;

            if (visiting.Contains(dependencyNode))
            {
                infos.LogError("Cyclic dependency found. A Node can not depend on itself.", node);
                return true;
            }

            if (visited.Contains(dependencyNode))
            {
                continue;
            }

            if (HasCycle(dependencyNode, visiting, visited, infos))
            {
                return true;
            }
        }

        visiting.Remove(node);
        visited.Add(node);

        return false;
    }

    /// <summary>
    /// Liest den Wert eines Input-Ports aus. Dies wird vom Importer verwendet.
    /// </summary>
    public static T ResolvePortValue<T>(IPort port)
    {
        var sourcePort = port.firstConnectedPort;

        switch (sourcePort?.GetNode())
        {
            case IConstantNode node:
                node.TryGetValue(out T constantValue);
                return constantValue;

            case IVariableNode node:
                node.variable.TryGetDefaultValue(out T variableValue);
                return variableValue;
        }

        if (sourcePort == null)
        {
            port.TryGetValue(out T embeddedValue);
            return embeddedValue;
        }

        return default;
    }
}