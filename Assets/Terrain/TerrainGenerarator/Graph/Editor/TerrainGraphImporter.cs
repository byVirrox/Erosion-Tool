using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.Editor;
using UnityEditor.AssetImporters;
using UnityEngine;

[ScriptedImporter(1, TerrainGraph.AssetExtension)]
public class TerrainGraphImporter : ScriptedImporter
{
    private Dictionary<INode, int> m_NodeToIdMap;
    private int m_NextId;

    public override void OnImportAsset(AssetImportContext ctx)
    {
        var editorGraph = GraphDatabase.LoadGraphForImporter<TerrainGraph>(ctx.assetPath);
        if (editorGraph == null) 
        { 
            Debug.LogError($"Failed to load TerrainGraph asset at path: {ctx.assetPath}"); 
            return; 
        }

        var outputNode = editorGraph.GetNodes().OfType<TerrainOutputNode>().FirstOrDefault();
        if (outputNode == null) 
        { 
            Debug.LogError($"The TerrainGraph at path '{ctx.assetPath}' must have a TerrainOutputNode."); 
            return; 
        }

        var runtimeGraph = ScriptableObject.CreateInstance<TerrainRuntimeGraph>();

        m_NodeToIdMap = new Dictionary<INode, int>();
        m_NextId = 1;

        var recipeNodes = new List<TerrainRuntimeNode>();
        var visitedNodes = new HashSet<INode>();

        TraverseAndBuildRecipe(outputNode, recipeNodes, visitedNodes);


        runtimeGraph.nodes = recipeNodes;

        ctx.AddObjectToAsset("RuntimeGraph", runtimeGraph);
        ctx.SetMainObject(runtimeGraph);
    }


    private void CollectAllNodes(INode node, List<INode> collection, HashSet<INode> visited)
    {
        if (node == null || !visited.Add(node)) return;

        foreach (var inputPort in node.GetInputPorts())
        {
            if (inputPort.isConnected && inputPort.firstConnectedPort != null)
            {
                CollectAllNodes(inputPort.firstConnectedPort.GetNode(), collection, visited);
            }
        }
        collection.Add(node);
    }


    private void TraverseAndBuildRecipe(INode node, List<TerrainRuntimeNode> recipeNodes, HashSet<INode> visited)
    {
        if (node == null || visited.Contains(node)) return;

        visited.Add(node);

        foreach (var inputPort in node.GetInputPorts())
        {
            if (inputPort.isConnected && inputPort.firstConnectedPort != null)
            {
                TraverseAndBuildRecipe(inputPort.firstConnectedPort.GetNode(), recipeNodes, visited);
            }
        }

        TerrainRuntimeNode runtimeNode = TranslateNodeToRuntimeNode(node);
        if (runtimeNode != null)
        {
            recipeNodes.Add(runtimeNode);
        }
    }

    private TerrainRuntimeNode TranslateNodeToRuntimeNode(INode editorNode)
    {
        if (!m_NodeToIdMap.ContainsKey(editorNode))
        {
            m_NodeToIdMap[editorNode] = m_NextId++;
        }
        int currentNodeId = m_NodeToIdMap[editorNode];

        TerrainRuntimeNode runtimeNode = null;

        switch (editorNode)
        {
            case TerrainFBMNode fbmNode:
                var fbmRuntimeNode = new FbmRuntimeNode();

                fbmRuntimeNode.offset = TerrainGraph.ResolvePortValue<Vector2>(fbmNode.GetInputPortByName(TerrainFBMNode.OFFSET_PORT));
                fbmRuntimeNode.scale = TerrainGraph.ResolvePortValue<float>(fbmNode.GetInputPortByName(TerrainFBMNode.SCALE_PORT));
                fbmRuntimeNode.xScale = TerrainGraph.ResolvePortValue<float>(fbmNode.GetInputPortByName(TerrainFBMNode.XSCALE_PORT));
                fbmRuntimeNode.yScale = TerrainGraph.ResolvePortValue<float>(fbmNode.GetInputPortByName(TerrainFBMNode.YSCALE_PORT));
                fbmRuntimeNode.heightScale = TerrainGraph.ResolvePortValue<float>(fbmNode.GetInputPortByName(TerrainFBMNode.HEIGHT_SCALE_PORT));
                fbmRuntimeNode.persistence = TerrainGraph.ResolvePortValue<float>(fbmNode.GetInputPortByName(TerrainFBMNode.PERSISTENCE_PORT));
                fbmRuntimeNode.lacunarity = TerrainGraph.ResolvePortValue<float>(fbmNode.GetInputPortByName(TerrainFBMNode.LACUNARITY_PORT));
                fbmRuntimeNode.octaves = TerrainGraph.ResolvePortValue<int>(fbmNode.GetInputPortByName(TerrainFBMNode.OCTAVES_PORT));
                fbmRuntimeNode.ridgedOctaves = TerrainGraph.ResolvePortValue<int>(fbmNode.GetInputPortByName(TerrainFBMNode.RIDGED_OCTAVES_PORT));
                fbmRuntimeNode.noiseType = TerrainGraph.ResolvePortValue<NoiseType>(fbmNode.GetInputPortByName(TerrainFBMNode.NOISE_TYPE_PORT));
                fbmRuntimeNode.fbmType = TerrainGraph.ResolvePortValue<FBMType>(fbmNode.GetInputPortByName(TerrainFBMNode.FBM_TYPE_PORT));

                runtimeNode = fbmRuntimeNode;
                break;

            case TerrainAddNode addNode:
                var addRuntimeNode = new AddRuntimeNode();
                IPort inputPortA = addNode.GetInputPortByName(TerrainAddNode.INPUT_A_PORT);
                if (inputPortA != null && inputPortA.isConnected && inputPortA.firstConnectedPort != null)
                {
                    addRuntimeNode.inputNodeIdA = m_NodeToIdMap[inputPortA.firstConnectedPort.GetNode()];
                }

                IPort inputPortB = addNode.GetInputPortByName(TerrainAddNode.INPUT_B_PORT);
                if (inputPortB != null && inputPortB.isConnected && inputPortB.firstConnectedPort != null)
                {
                    addRuntimeNode.inputNodeIdB = m_NodeToIdMap[inputPortB.firstConnectedPort.GetNode()];
                }
                runtimeNode = addRuntimeNode;
                break;

            case OperationNode opNode:
                var opRuntimeNode = new OperationRuntimeNode();

                INodeOption operationOption = opNode.GetNodeOptionByName(opNode.OPERATION_NAME); 
                operationOption.TryGetValue<OperationType>(out var selectedOperation);
                opRuntimeNode.opType = selectedOperation;

                switch (selectedOperation)
                {
                    case OperationType.Add:
                    case OperationType.Subtract:
                    case OperationType.Multiply:
                    case OperationType.Divide:
                        IPort portA_tex = opNode.GetInputPortByName(OperationNode.INPUT_A_TEX_PORT);
                        if (portA_tex != null && portA_tex.isConnected)
                        {
                            opRuntimeNode.inputNodeIdA = m_NodeToIdMap[portA_tex.firstConnectedPort.GetNode()];
                        }

                        IPort portB_tex = opNode.GetInputPortByName(OperationNode.INPUT_B_TEX_PORT);
                        if (portB_tex != null && portB_tex.isConnected)
                        {
                            opRuntimeNode.inputNodeIdB = m_NodeToIdMap[portB_tex.firstConnectedPort.GetNode()];
                        }
                        break;


                    case OperationType.AddByValue:
                    case OperationType.SubtractByValue:
                    case OperationType.MultiplyByValue:
                    case OperationType.DivideByValue:
                    case OperationType.Power:
                        IPort portA_val = opNode.GetInputPortByName(OperationNode.INPUT_A_TEX_PORT);
                        if (portA_val != null && portA_val.isConnected)
                        {
                            opRuntimeNode.inputNodeIdA = m_NodeToIdMap[portA_val.firstConnectedPort.GetNode()];
                        }

                        IPort valuePort = opNode.GetInputPortByName(OperationNode.INPUT_VAL_PORT);
                        if (valuePort != null)
                        {
                            opRuntimeNode.value = TerrainGraph.ResolvePortValue<float>(valuePort);
                        }
                        break;
                }

                runtimeNode = opRuntimeNode;
                break;

            case SplineNode splineNode:
                var splineRuntimeNode = new SplineRuntimeNode();


                splineRuntimeNode.inputMin = splineNode.inputMin;
                splineRuntimeNode.inputMax = splineNode.inputMax;

                IPort inputTexture = splineNode.GetInputPortByName(SplineNode.INPUT_PORT_NAME);
                if (inputTexture != null && inputTexture.isConnected && inputTexture.firstConnectedPort != null)
                {
                    splineRuntimeNode.inputNodeId = m_NodeToIdMap[inputTexture.firstConnectedPort.GetNode()];
                }

                AnimationCurve curve = splineNode.GetSpline();
                int sampleResolution = splineNode.GetSampleRate(); 

                splineRuntimeNode.bakedCurveSamples = new float[sampleResolution];
                for (int i = 0; i < sampleResolution; i++)
                {
                    float t = i / (float)(sampleResolution - 1);
                    splineRuntimeNode.bakedCurveSamples[i] = curve.Evaluate(t);
                }

                runtimeNode = splineRuntimeNode;
                break;

            case TerrainOutputNode outputNode:
                var outputRuntimeNode = new OutputRuntimeNode();
                IPort inputPort = outputNode.GetInputPortByName(TerrainOutputNode.INPUT_PORT_NAME);
                if (inputPort != null && inputPort.isConnected && inputPort.firstConnectedPort != null)
                {
                    outputRuntimeNode.finalInputNodeId = m_NodeToIdMap[inputPort.firstConnectedPort.GetNode()];
                }
                runtimeNode = outputRuntimeNode;
                break;
        }

        if (runtimeNode != null)
        {
            runtimeNode.nodeId = currentNodeId;
        }

        return runtimeNode;
    }
}