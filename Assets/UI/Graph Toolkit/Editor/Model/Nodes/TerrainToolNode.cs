using System;
using Unity.GraphToolkit.Editor;

namespace Unity.GraphToolkit.Samples.VisualTerrainTool.Editor
{
    /// <summary>
    /// Terrain tool Base Node model.
    /// </summary>
    [Serializable]
    internal abstract class TerrainToolNode : Node
    {
        public const string EXECUTION_PORT_DEFAULT_NAME = "ExecutionPort";

        /// <summary>
        /// Defines common input and output execution ports for all nodes in the Visual Terrain tool.
        /// </summary>
        /// <param name="context">The scope to define the node.</param>
        protected void AddInputOutputExecutionPorts(IPortDefinitionContext context)
        {
            context.AddInputPort(EXECUTION_PORT_DEFAULT_NAME)
                .WithDisplayName(string.Empty)
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            context.AddOutputPort(EXECUTION_PORT_DEFAULT_NAME)
                .WithDisplayName(string.Empty)
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }
    }
}
